/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */
using System;
using System.Drawing;
using System.IO;

namespace Haley.RotateText.Definition
{

	public enum VAlignment { Top, Middle, Bottom }
	public enum HAlignment { Left, Center, Right }

	
	/// <summary>
	/// Holds the defintion for a rotated text image
	/// </summary>
	[Serializable]
	public class RotateTextDefn : IDisposable
	{
	    public Font Font = new Font("Verdana",12,GraphicsUnit.Point);
		public Color TextColor = Color.Black;
		public Color BackgroundColor = Color.White;
		public string Text = "";

		public StringAlignment Align = StringAlignment.Near; // the paragraph alignment of the text
		public VAlignment VerticalAlignment = VAlignment.Top; // the vertical alignment of the paragraph within the background image
		public HAlignment HorizontalAlignment = HAlignment.Left; // the horizontal alignment of the paragraph within the background image
		
		public string BackgroundImageFile; // file to use for the background
		public byte[] BackgroundImageBytes; // byte array of image data to load for background image
		public Bitmap BackgroundImageBitmap; // in memory bitmpa image for background
		public bool ResizeBackground; // should be resize the background to fit to size required or just crop a section from the top left
		public bool MaintainAspect; // should we maintain the aspect ratio when resizing the image
		public bool UseScalingDefinition; // should we use the scaling defnintion when deciding how to resize the image
		public ScalingDefn ScalingDefinition = new ScalingDefn(); // advanced image resizing based on defined sections
		public Border TextPadding = new Border(); // padding to apply to text rectangle
		
		
		public int Width; // fixed width for image, zero to determine width from text rectangle size plus padding
		public int Height; // fixed height for image, zero to determine height from text rectangle size plus padding
		private int _angle; // angle in degrees through which to rotate the image, horizontal in zero
		public int Angle
		{
			get
			{
				return _angle;
			}
			set
			{
				_angle = value;
				_angle = _angle % 360;
				if (_angle<0)
					_angle = _angle + 360;
			}
		}
	
	
		public void Dispose()
		{
			// if they are present dispose of font and bitmap resources and large objects
			if (Font!=null)
			{
				Font.Dispose();
				Font = null;
			}
			if (BackgroundImageBitmap!=null)
			{
				BackgroundImageBitmap.Dispose();
				BackgroundImageBitmap = null;
			}			
			BackgroundImageBytes = null;
			ScalingDefinition = null;
			TextPadding = null;
		}

	}
}
