using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Checks if the object can be assigned to another type.  True means this object is one of the following:
        /// Is the same type as the tested type; is a subclass of the tested type; implements the tested type (if the 
        /// tested type is an interface).
        /// </summary>
        /// <param name="thisObject"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static bool IsAssignableTo(this Type thisObject, Type targetType)
        {
            if (thisObject == null)
            {
                throw new ArgumentNullException(nameof(thisObject));
            }
            else if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            return thisObject == targetType ||
                targetType.IsAssignableFrom(thisObject) ||
                targetType.IsInterface && thisObject.GetInterface(targetType.Name) != null;
        }

        /// <summary>
        /// Checks if a type is a nullable Enum.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>From https://stackoverflow.com/questions/2723048/checking-if-type-instance-is-a-nullable-enum-in-c-sharp </remarks>
        public static bool IsNullableEnum(this Type t)
        {
            Type u = Nullable.GetUnderlyingType(t);
            return (u != null) && u.IsEnum;
        }
    }
}
