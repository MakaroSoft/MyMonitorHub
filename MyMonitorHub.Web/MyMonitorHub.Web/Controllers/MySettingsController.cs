using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
    public class MySettingsController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IMonitorAuthService _authService;

        public MySettingsController(IDbContextScopeFactory contextScopeFactory, IWebHostEnvironment env, IMonitorAuthService authService)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
            _env = env;
            _authService = authService;
        }

        public class MyModel
        {
            public string? Url { get; set; }
            public string? UrlMedium { get; set; }
            public string? UrlTest { get; set; }
        }

        public class Coords
        {
            public int x { get; set; }
            public int y { get; set; }
            public int x2 { get; set; }
            public int y2 { get; set; }
            public int w { get; set; }
            public int h { get; set; }
        }

        [HttpPost]
        public JsonResult CropAndSave(string file, int width, int height, Coords coords)
        {
            AllInOne(file, width, height, coords);
            return Json("");
        }

        [HttpPost]
        public JsonResult UseGravatar()
        {
            var avatarDir = Path.Combine(_env.WebRootPath, "Images", "Avatars");
            var imageSrc = Path.Combine(avatarDir, $"user-{Helper.UserId}.png");
            if (System.IO.File.Exists(imageSrc))
                System.IO.File.Delete(imageSrc);

            var userModel = GetModel(Helper.UserId);
            var src = GetGravatarSource(userModel!.Email);
            return Json(src);
        }

        private void AllInOne(string file, int width, int height, Coords coords)
        {
            var avatarDir = Path.GetFullPath(Path.Combine(_env.WebRootPath, "Images", "Avatars"));
            var sourcePath = Path.GetFullPath(Path.Combine(avatarDir, file));

            // Reject any path that escapes the avatars directory
            if (!sourcePath.StartsWith(avatarDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file path.");

            var destinationPath = Path.Combine(avatarDir, $"User-{Helper.UserId}.png");

            // Scope the bitmaps so every GDI+ handle on `sourcePath` is released
            // before we try to delete the temp file below.
            {
                using var sourceImage = new Bitmap(sourcePath);

                // Browsers honor EXIF orientation when displaying images, but System.Drawing
                // does not — so the raw pixels can be 90/180/270° rotated (or mirrored)
                // relative to what the user saw in Jcrop. Without this, the crop selection
                // ends up against the unrotated pixel grid and grabs the wrong region.
                ApplyExifOrientation(sourceImage);

                using var shrunkToSizeOnScreen = Shrink(width, height, sourceImage);

                using var destinationImage = new Bitmap(coords.w, coords.h, shrunkToSizeOnScreen.PixelFormat);
                using (var g = Graphics.FromImage(destinationImage))
                {
                    g.DrawImage(shrunkToSizeOnScreen, new Rectangle(0, 0, coords.w, coords.h),
                        new Rectangle(coords.x, coords.y, coords.w, coords.h), GraphicsUnit.Pixel);
                }

                using var finalImage = Shrink(160, 160, destinationImage);
                finalImage.Save(destinationPath, ImageFormat.Png);
            }

            // Clean up the temp upload now that the final avatar has been saved.
            // Swallow IO errors — a stuck temp file is annoying but never fatal.
            try
            {
                if (System.IO.File.Exists(sourcePath))
                    System.IO.File.Delete(sourcePath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Reads the EXIF "Orientation" tag (0x0112) from <paramref name="image"/> and
        /// physically rotates/flips the pixels so the bitmap matches what a browser
        /// would render. The orientation tag is removed afterwards to avoid any
        /// double-rotation downstream.
        /// </summary>
        private static void ApplyExifOrientation(Image image)
        {
            const int ExifOrientationId = 0x0112;
            if (!image.PropertyIdList.Contains(ExifOrientationId)) return;

            var prop = image.GetPropertyItem(ExifOrientationId);
            if (prop?.Value == null || prop.Value.Length == 0) return;

            int orientation = prop.Value[0];

            var rotate = orientation switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.Rotate180FlipX,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone
            };

            if (rotate != RotateFlipType.RotateNoneFlipNone)
                image.RotateFlip(rotate);

            image.RemovePropertyItem(ExifOrientationId);
        }

        public static Bitmap Shrink(int Width, int Height, Bitmap sourceImage)
        {
            float percentageResize = 0;
            int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
            int sourceWidth = sourceImage.Width, sourceHeight = sourceImage.Height;

            float percentageResizeW = (float)Width / sourceWidth;
            float percentageResizeH = (float)Height / sourceHeight;

            if (percentageResizeH < percentageResizeW)
            {
                percentageResize = percentageResizeW;
                destY = Convert.ToInt16((Height - (sourceHeight * percentageResize)) / 2);
            }
            else
            {
                percentageResize = percentageResizeH;
                destX = Convert.ToInt16((Width - (sourceWidth * percentageResize)) / 2);
            }

            int destWidth = (int)Math.Round(sourceWidth * percentageResize);
            int destHeight = (int)Math.Round(sourceHeight * percentageResize);

            var objBitmap = new Bitmap(Width, Height, sourceImage.PixelFormat);
            objBitmap.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            using var objGraphics = Graphics.FromImage(objBitmap);
            objGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            objGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            objGraphics.DrawImage(sourceImage,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);
            return objBitmap;
        }

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

        [RequestSizeLimit(MaxAvatarBytes)]
        public IActionResult SaveUploadedFile()
        {
            var fName = "";
            foreach (var file in Request.Form.Files)
            {
                if (file == null || file.Length <= 0) continue;

                if (file.Length > MaxAvatarBytes)
                    return BadRequest("File exceeds the maximum allowed size of 5 MB.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                    return BadRequest("Only image files (.jpg, .jpeg, .png, .gif) are allowed.");

                // Server-generated filename — never trust client FileName beyond the extension
                fName = $"temp-{Helper.UserId}{extension}";

                var pathString = Path.Combine(_env.WebRootPath, "Images", "Avatars");
                if (!Directory.Exists(pathString))
                    Directory.CreateDirectory(pathString);

                var path = Path.Combine(pathString, fName);
                using var stream = System.IO.File.Create(path);
                file.CopyTo(stream);
            }
            return Json(new { Message = fName });
        }

        private void SetAvatar(int userId, string email)
        {
            var physicalPath = Path.Combine(_env.WebRootPath, "Images", "Avatars", $"user-{userId}.png");
            if (System.IO.File.Exists(physicalPath))
            {
                // Cache-bust the local avatar with the file's last-write timestamp so a
                // freshly uploaded picture is picked up immediately while unchanged
                // files still get cached normally by the browser.
                var version = System.IO.File.GetLastWriteTimeUtc(physicalPath).Ticks;
                ViewBag.Avatar = Url.Content($"~/Images/Avatars/user-{userId}.png?v={version}");
            }
            else
            {
                ViewBag.Avatar = GetGravatarSource(email.ToLower());
            }
        }

        private string GetGravatarSource(string email)
        {
            var hash = ComputeMd5(email);
            return $"https://www.gravatar.com/avatar/{hash}?s=100&d=retro";
        }

        private static string ComputeMd5(string input)
        {
            var data = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(data).ToLower();
        }

        [HttpGet]
        public new IActionResult Profile()
        {
            var userModel = GetModel(Helper.UserId);
            SetAvatar(userModel!.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;

            using (var scope = _contextScopeFactory.Create())
            {
                var user = scope.Get<User>().FirstOrDefault(x => x.UserId == Helper.UserId);
                var twoFactorEnabled = user?.TwoFactorEnabled == true;
                ViewBag.TwoFactorEnabled = twoFactorEnabled;
                ViewBag.RecoveryCodesRemaining = twoFactorEnabled
                    ? scope.Get<UserRecoveryCode>().Count(x => x.UserId == Helper.UserId && x.UsedAt == null)
                    : 0;
            }

            return View(userModel);
        }

        [HttpPost]
        public IActionResult Reset2Fa()
        {
            _authService.Reset2Fa(Helper.UserId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Profile(int? id, UserModel model)
        {
            if (id == null) id = Helper.UserId;

            // Strip any auto-generated model-binder errors on the password fields
            // — they are optional and must never surface as "required" in the UI.
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            // The password fields are optional.  Null/empty means "do not change
            // the password" — ignore them and save everything else.  Only validate
            // the password if the user actually typed one.
            var changingPassword = !string.IsNullOrEmpty(model.Password)
                                || !string.IsNullOrEmpty(model.ConfirmPassword);

            var passwordError = (string?)null;
            if (changingPassword)
            {
                if (model.Password != model.ConfirmPassword)
                    passwordError = "The new password and confirmation password do not match.";
                else if ((model.Password ?? string.Empty).Length < 8)
                    passwordError = "Password must be at least 8 characters long.";
            }

            // Only a bad password aborts the save — every other field is saved
            // regardless of ModelState (a duplicate Email input + data annotations
            // can otherwise make ModelState.IsValid false for no real reason).
            var passed = false;
            if (passwordError == null)
            {
                using (var scope = _contextScopeFactory.Create())
                {
                    var user = scope.Get<User>().FirstOrDefault(x => x.UserId == id.Value);
                    UserSecurityCheck(user != null, id, UserMode.Edit);

                    if (user != null)
                    {
                        if (!ViewBag.PasswordIsDisabled && changingPassword)
                            user.Password = PasswordHasher.Hash(model.Password);

                        user.FirstName = model.FirstName;
                        user.LastName  = model.LastName;
                        user.HomePhone = model.HomePhone;
                        user.WorkPhone = model.WorkPhone;
                        user.CellPhone = model.CellPhone;
                        scope.SaveChanges();
                        passed = true;
                    }
                }
            }
            else
            {
                ModelState.AddModelError(nameof(model.Password), passwordError);
            }

            if (passed)
                model = GetModel(id)!;
            else
            {
                var orig = GetModel(id);
                model.UserId = orig!.UserId;
            }

            var result = Profile();
            ViewBag.success = passed;
            return result;
        }

        private UserModel? GetModel(int? id)
        {
            id ??= Helper.UserId;
            using var scope = _contextScopeFactory.Create();
            var user = scope.Get<User>().FirstOrDefault(x => x.UserId == id.Value);
            if (user == null) return null;
            return new UserModel
            {
                CellPhone = user.CellPhone,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HomePhone = user.HomePhone,
                WorkPhone = user.WorkPhone,
                UserId = user.UserId
            };
        }

        private void UserSecurityCheck(bool found, int? id, UserMode mode)
        {
            if (!found) throw new SecurityException("User does not exist");

            ViewBag.saveDisabled = "";
            ViewBag.passwordDisabled = new { @class = "form-control" };
            ViewBag.PasswordIsDisabled = false;
            if (!Helper.IsAdministrator && !Helper.IsOwner && id != Helper.UserId)
            {
                ViewBag.PasswordIsDisabled = true;
                ViewBag.PasswordDisabled = new { @class = "form-control", disabled = "Disabled" };
            }

            if (id == Helper.UserId) return;

            if (!Authorizer.Authorize(Permissions.CanViewAnyone))
                throw new SecurityException(Permissions.CanViewAnyone.FailMessage);

            if (!Authorizer.Authorize(Permissions.CanEditAnyone))
            {
                if (mode == UserMode.View)
                    ViewBag.saveDisabled = "Disabled = 'Disabled'";
                else
                    throw new SecurityException(Permissions.CanEditAnyone.FailMessage);
            }
        }

        private enum UserMode { View, Edit }

        public static string HashEmailForGravatar(string email)
        {
            var data = MD5.HashData(Encoding.UTF8.GetBytes(email));
            var sBuilder = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
                sBuilder.Append(i.ToString("x2"));
            return sBuilder.ToString();
        }
    }
}
