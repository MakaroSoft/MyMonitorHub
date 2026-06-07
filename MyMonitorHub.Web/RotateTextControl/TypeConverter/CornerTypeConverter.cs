/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */
using System;
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using Haley.RotateText.Definition;

namespace Haley.RotateText.TypeConverter
{
	
	/// <summary>
	/// TypeConverter for the Corner object required for Expandable use in the property grid
	/// </summary>
	public class CornerConverter : ExpandableObjectConverter
	{
	    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			string [] names = {"Width", "Height"};
			return TypeDescriptor.GetProperties(value,attributes).Sort(names);
		}

                                 
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType) 
		{
			if (destType == typeof(string) && value is Corner) 
			{
				var c = (Corner)value;
				//return "Corner";
				return c.Width + "," + c.Height;
			}
			if(destType == typeof(InstanceDescriptor))
			{
			    var constructor = typeof(Corner).GetConstructor(new[] {typeof(int),typeof(int)});
				var c = (Corner)value;
				return new InstanceDescriptor(constructor,
					new Object[] {c.Width, c.Height});
			}
			return base.ConvertTo(context, culture, value, destType);
		}   	

	}
}
