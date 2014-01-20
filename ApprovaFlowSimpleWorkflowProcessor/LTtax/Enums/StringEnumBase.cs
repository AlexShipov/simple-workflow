namespace LTtax.Enums
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>    
    /// implements type-safe-enum pattern
    /// </summary>
    public class StringEnumBase
    {
        protected string m_value;
        protected static readonly Dictionary<string, StringEnumBase> m_instance = new Dictionary<string, StringEnumBase>(StringComparer.CurrentCultureIgnoreCase);

        public override string ToString()
        {
            return this.m_value;
        }

        public static implicit operator StringEnumBase(string value)
        {
            StringEnumBase result;
            if (m_instance.TryGetValue(value, out result))
                return result;
            else
                throw new InvalidCastException();
        }

        public static implicit operator string(StringEnumBase value)
        {
            return value.m_value;
        }

        /// <summary>
        /// Initializes look dictionary for casting string to used in extending class static constructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected static void InitLookup<T>() where T : StringEnumBase
        {
            var fields = typeof(T).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            fields.ToList().ForEach(
                item => m_instance.Add(item.GetValue(null).ToString(), (StringEnumBase)item.GetValue(null))
            );
        }

        protected StringEnumBase(string value)
        {
            this.m_value = value;
        }
    }
}