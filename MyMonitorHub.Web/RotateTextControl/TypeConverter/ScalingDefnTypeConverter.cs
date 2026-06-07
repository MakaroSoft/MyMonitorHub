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
	/// TypeConverter for the ScalingDefn require to use the expandable property grid feature
	/// </summary>
	public class ScaleDefnConverter : ExpandableObjectConverter
	{
	    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			// return the list of properties ordering as we want
			string [] names = {"TopBorder", "BottomBorder", "LeftBorder", "RightBorder", "TopLeft", "TopRight", "BottomRight", "BottomLeft"};
			var pdc =  TypeDescriptor.GetProperties(value,attributes).Sort(names);
			return pdc;
		}
                                 
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType) 
		{
			if (destType == typeof(string) && value is ScalingDefn) 
			{
			    return "Expand for values";
				// this would be a rather meaning less sting of numbers so I chose not to do it
				//return sd.TopBorder.ToString() + "," + sd.BottomBorder.ToString()+ "," + sd.LeftBorder.ToString()+ "," + sd.RightBorder.ToString() + "," +
					//sd.TopLeft.ToString() + "," + sd.TopRight.ToString() + "," + sd.BottomRight.ToString() + "," + sd.BottomLeft.ToString();
			}
			if(destType == typeof(InstanceDescriptor))
			{
			    var constructor = typeof(ScalingDefn).GetConstructor(new[] {typeof(Corner),typeof(Corner),typeof(Corner),typeof(Corner), typeof(int),typeof(int),typeof(int),typeof(int)});
				var sd = (ScalingDefn)value;
				return new InstanceDescriptor(constructor,
					new Object[] {sd.TopLeft, sd.TopRight, sd.BottomRight, sd.BottomLeft, sd.TopBorder, sd.BottomBorder, sd.LeftBorder, sd.RightBorder});
			}

			return base.ConvertTo(context, culture, value, destType);
		}   	
	}
}
