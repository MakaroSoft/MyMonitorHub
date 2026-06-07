/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */
using System;
using System.ComponentModel;
using Haley.RotateText.TypeConverter;

namespace Haley.RotateText.Definition
{
	/// <summary>
	/// This holds the values that define a set of borders, margins, padding etc
	/// </summary>
	[Serializable,
	TypeConverter(typeof(BorderConverter))]
	public class Border
	{
		
		private int _top;
		private int _bottom;
		private int _left;
		private int _right;

		public Border()
		{
			
		}

		public Border (int top, int bottom, int left, int right)
		{
			_top = top;
            _bottom = bottom;
            _left = left;
            _right = right;
		}

		public override string ToString()
		{
			return Top + "," + Bottom+ "," + Left+ "," + Right;
		}
		
		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Top
		{
			get { return _top; }
			set { _top = value; }
		}

		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Bottom
		{
			get { return _bottom; }
			set { _bottom = value; }
		}
		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Left
		{
			get { return _left; }
			set { _left = value; }
		}
		[NotifyParentProperty(true), 
		RefreshProperties(RefreshProperties.All)]
		public int Right
		{
			get { return _right; }
			set { _right = value; }
		}
		
	}

}
