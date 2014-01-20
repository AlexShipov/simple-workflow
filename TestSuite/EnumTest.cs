using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using LTtax.Enums;

using ApprovaFlowSimpleWorkflowProcessor.LTtax;

namespace TestSuite
{
    [TestClass]
    public class EnumTest
    {
        public sealed class MyEnum : StringEnumBase
        {
            static MyEnum()
            {
                StringEnumBase.InitLookup<MyEnum>();
            }

            private MyEnum(string value)
                : base(value)
            {
            }

            public static readonly StringEnumBase Add = new MyEnum("Add");
            public static readonly StringEnumBase Delete = new MyEnum("Delete");
            public static readonly StringEnumBase Test = new MyEnum("Test");
        }

        public class TestContainer
        {
            private StringEnumBase m_status;
            private StringEnumBase m_trigger;

            public string Status { get { return m_status; } set { m_status = value; } }
            public string Trigger { get { return m_trigger; } set { m_trigger = value; } }

            public TestContainer()
            {
                Status = MyEnum.Add;
                Trigger = MyEnum.Delete;
            }

            public TestContainer(StringEnumBase status, StringEnumBase trigger)
            {
                this.Status = status;
                this.Trigger = trigger;
            }
        }

        [TestMethod]
        public void TestEnum()
        {
            //{"Status":"Add","Trigger":"Delete"}

            var test = new TestContainer();

            var json = JsonConvert.SerializeObject(test);

            var obj = JsonConvert.DeserializeObject<TestContainer>(json);

            AssertHelper.Throws<InvalidCastException>(() => new TestContainer("Add", "wtf man"));

            json = @"{""Status"":""Add"",""Trigger"":""test""}";

            obj = JsonConvert.DeserializeObject<TestContainer>(json);

            Assert.AreEqual(MyEnum.Test, (StringEnumBase)obj.Trigger);
        }
    }
}
