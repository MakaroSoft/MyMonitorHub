/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */
using System;
using System.ComponentModel;
using Haley.RotateText.TypeConverter;

namespace Haley.RotateText.Definition
{
	/// <summary>
	/// Holds values that define a horizontal and vertical offset into a corner, hence defining a rectangle from the corner to a point in the image
	/// The corners of the background image are not scaled, they are copied to the corners of the resulting image as is.
	/// </summary>
	[Serializable,
	TypeConverter(typeof(CornerConverter))]
	public class Corner
	{
		public Corner()
		{
			
		}

		public Corner (int width, int height)
		{
			m_Width = width;
			m_Height = height;
		}

		public override string ToString()
		{
			return this.Width.ToString() + "," + this.Height.ToString();
		}
		private int m_Width=0;
		private int m_Height=0;

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Width
		{
			get { return m_Width; }
			set { m_Width = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Height
		{
			get { return m_Height; }
			set { m_Height = value; }
		}
	}

}
