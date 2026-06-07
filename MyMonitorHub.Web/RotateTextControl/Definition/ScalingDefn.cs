/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */
using System;
using System.ComponentModel;
using Haley.RotateText.TypeConverter;

namespace Haley.RotateText.Definition
{
	/// <summary>
	/// Defines how we are going to cut up and scale the background image in terms of
	/// 4 corners that are not scaled
	/// 4 borders that are scaled along the edges inbetween the border
	/// a center that is cut from the biggest rectangle in the source image that does overlap any of the previous section and stretched over the entire target image before the other areas are applied
	/// </summary>
	[Serializable,
	TypeConverter(typeof(ScaleDefnConverter))]
	public class ScalingDefn
	{
		public ScalingDefn()
		{

		}

		public ScalingDefn(Corner topLeft, Corner topRight, Corner bottomRight, Corner bottomLeft, int topBorder, int bottomBorder, int leftBorder, int rightBorder)
		{
			m_TopLeft = topLeft;
			m_TopRight = topRight;
			m_BottomRight = bottomRight;
			m_BottomLeft = bottomLeft;
			m_TopBorder = topBorder;
			m_BottomBorder = bottomBorder;
			m_LeftBorder = leftBorder;
			m_RightBorder = rightBorder;
		}

	
		private Corner m_TopLeft = new Corner();
		private Corner m_TopRight = new Corner();
		private Corner m_BottomLeft = new Corner();
		private Corner m_BottomRight = new Corner();
		
		private int m_TopBorder=0;
		private int m_BottomBorder=0;
		private int m_LeftBorder=0;
		private int m_RightBorder=0;

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
		Description("The area in the top left of the image that should be copied without scaling")]
		public Corner TopLeft
		{
			get { return m_TopLeft; }
			set { m_TopLeft = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
		Description("The area in the top right of the image that should be copied without scaling")]
		public Corner TopRight
		{
			get { return m_TopRight; }
			set { m_TopRight = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
		Description("The area in the bottom left of the image that should be copied without scaling")]
		public Corner BottomLeft
		{
			get { return m_BottomLeft; }
			set { m_BottomLeft = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
		Description("The area in the bottom right of the image that should be copied without scaling")]
		public Corner BottomRight
		{
			get { return m_BottomRight; }
			set { m_BottomRight = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		Description("The height of the area between the top corners that should be scaled along the top edge of the target image")]		
		public int TopBorder
		{
			get { return m_TopBorder; }
			set { m_TopBorder = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		Description("The height of the area between the bottom corners that should be scaled along the bottom edge of the target image")]
		public int BottomBorder
		{
			get { return m_BottomBorder; }
			set { m_BottomBorder = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		Description("The width of the area between the left hand corners that should be scaled along the left edge of the target image")]
		public int LeftBorder
		{
			get { return m_LeftBorder; }
			set { m_LeftBorder = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All),
		Description("The width of the area between the right hand corners that should be scaled along the right edge of the target image")]
		public int RightBorder
		{
			get { return m_RightBorder; }
			set { m_RightBorder = value; }
		}
		
	}
}
