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
	/// TypeConvertor for the Border object, this is required to enable expandable use of the property grid
	/// </summary>
	public class BorderConverter : ExpandableObjectConverter
	{
	    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			string [] names = {"Top", "Bottom","Left","Right"};
			return TypeDescriptor.GetProperties(value,attributes).Sort(names);
		}

                                 
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType) 
		{
			if (destType == typeof(string) && value is Corner)
			{
			    var b = (Border)value;
			    //return "Expand for values";
				return b.Top + "," + b.Bottom + "," + b.Left + "," + b.Right;
			}
		    if(destType == typeof(InstanceDescriptor))
			{
			    var constructor = typeof(Corner).GetConstructor(new[] {typeof(int),typeof(int),typeof(int),typeof(int)});
				var b = (Border)value;
				return new InstanceDescriptor(constructor,
					new Object[] {b.Top, b.Bottom, b.Left, b.Right});
			}
			return base.ConvertTo(context, culture, value, destType);
		}   	

	}
}
