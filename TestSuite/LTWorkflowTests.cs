using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTtax.Workflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestSuite
{

    internal static class AssertHelper
    {        
        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        public static void Throws<TException>(Action action)
        {
            try
            {
                action();
                Assert.Fail("Exception not thrown.");
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(TException))
                {
                    Assert.Fail(string.Format("Expected {0} thrown {1}", typeof(TException).Name,  ex.GetType().Name));
                }
            }
        }
    }

    [TestClass]
    public class LTWorkflowTests
    {
        /// <summary>
        /// Test transition selection objects If and default methods.
        /// </summary>
        [TestMethod]        
        public void TransitionSelectionTest()
        {
            var state = "testState";
            var trigger = "testTrigger";
            var state2 = "testState2";
            

            var trSelection = new Workflow<string, string>(state)
                    .Configure(state)
                    .On(trigger)
                    .Do(() => state);

            // unhandled status throws exception
            AssertHelper.Throws<ArgumentException>(() => {
                var wf = new Workflow<string, string>(state);
                wf.Configure(state)
                    .On(trigger)
                        .Do(() => state)
                            .Accepts(state2);
                wf.Fire(trigger);
            });

            // may add same status multiple times to accepts list
            AssertHelper.DoesNotThrow(() => trSelection.Accepts(state2));
            AssertHelper.DoesNotThrow(() => trSelection.Accepts(state2));

            // test for permit reentrant states
            AssertHelper.DoesNotThrow(() =>
                {
                    var wf = new Workflow<string, string>(state);

                    Assert.AreEqual(wf.Status, state);

                    var trSelection2 = wf
                            .Configure(state)
                                .On(trigger)
                                    .Do(() => state);

                    trSelection2.Accepts(state);

                    wf.Fire(trigger);

                    Assert.AreEqual(wf.Status, state);
                });            
        }
        
        /// <summary>
        /// Test transition selection objects Fluent methods On AutoFire.
        /// </summary>
        [TestMethod]        
        public void TransitionSelectionFluentTest()
        {
            var state = "testState";
            var trigger = "testTrigger";
            var trigger2 = "testTrigger2";
            var trigger3 = "testTrigger3";
            var state2 = "testState2";
            
            bool trigger1Fired = false;
            bool trigger2Fired = false;

            // configure workflow with 3 triggers and 2 states
            Func<Workflow<string, string>> getWf = 
                () =>
            {
                var tmpwf = new Workflow<string, string>(state);
                tmpwf
                .Configure(state)
                    .On(trigger)
                        .Do(() => { trigger1Fired = true; return state; })
                            .Accepts(state2)
                            .Accepts(state)
                    .AutoFire(trigger2)
                        .Do(() => { trigger2Fired = true; return state2; })
                            .Accepts(state2)
                    .On(trigger3, state2);

                return tmpwf;
            };

            Assert.IsFalse(trigger1Fired);
            Assert.IsFalse(trigger2Fired);
            // trigger trigger causes to transition to state1 which fires an auto transition to state 2
            Assert.AreEqual(state2, getWf().Fire(trigger).Status);
            Assert.IsTrue(trigger1Fired);
            Assert.IsTrue(trigger2Fired);
        }
        
        [TestMethod]        
        public void TransitionActionTest()
        {
            var state = "testState";
            var state2 = "testState2";
            var trigger = "testTrigger";
            
            // transition action may not be null
            AssertHelper.Throws<ArgumentNullException>
                (() => new Workflow<string, string>(state)
                    .Configure(state)
                    .On(trigger)
                    .Do(null));

            
            var test2 = new Workflow<string, string>(state)
               .Configure(state)
               .On(trigger);

            // Do may not be called more than once on a transition
            AssertHelper.Throws<ArgumentException>
                (() =>
                {
                    test2.Do(() => state).Accepts(state);
                    test2.Do(() => state).Accepts(state2);
                });

            bool actionFired = false;
            bool actionFired2 = false;

            Func<string> testAction = ( ) => 
            {
                actionFired = true;
                return state;
            };

            Func<string> testAction2 = () =>
            {
                actionFired2 = true;
                return state2;
            };

            // calling Do without finalization 
            // (If Default ... etc) does not register a transition
            var wf = new Workflow<string, string>(state);
            var action = wf.Configure(state).On(trigger);
            action.Do(testAction);
            AssertHelper.Throws<InvalidOperationException>(() => wf.Fire(trigger));

            // calling Do on an "unregistered" action will reset its action method
            // and not raise an error
            action.Do(testAction2).Accepts(state2);
            wf.Fire(trigger);
            Assert.AreEqual(wf.Status, state2);
            Assert.IsFalse(actionFired);
            Assert.IsTrue(actionFired2);
        }
        
        [TestMethod]        
        public void TransitionTest()
        {
            var state = "testState";
            var state2 = "state2";
            var trigger = "testTrigger";

            var transition1 = new Workflow<string, string>(state).Configure(state);
            // initial state for auto fire is false
            Assert.IsFalse(transition1.IsAutoFire);
                        
            // autofire assigns trigger and sets as utofire
            var transition2 = new Workflow<string, string>(state).Configure(state);
            transition2.AutoFire(trigger);
            Assert.IsTrue(transition2.IsAutoFire);
            
            // Over loaded on assigns trigger sets autofire to false and sets
            // transition as a static transition
            var transition3 = new Workflow<string, string>(state).Configure(state);
            transition3.On(trigger, state2);
            Assert.IsFalse(transition3.IsAutoFire);
        }
                
        [TestMethod]        
        public void InvalidTransitionTest()
        {
            var state = "testState";
            var trigger = "testTrigger";

            // transition action may not be null
            AssertHelper.Throws<ArgumentException>
                (() => new Workflow<string, string>(state)
                    .Configure(state)
                    .On(null));

            AssertHelper.Throws<ArgumentException>
                (() => new Workflow<string, string>(state)
                    .Configure(state)
                    .AutoFire(null));

            AssertHelper.Throws<ArgumentException>
                (() => new Workflow<string, string>(state)
                    .Configure(state)
                    .On(null, null));

            AssertHelper.Throws<ArgumentException>
                (() => new Workflow<string, string>(state)
                    .Configure(state)
                    .On(trigger, null));

            // On may not be called multiple times for the same tronsition
            AssertHelper.Throws<ArgumentException>
                (() =>
                {
                    var test = new Workflow<string, string>(state)
                    .Configure(state);

                    test.On(trigger);
                    test.On("someTrigger");
                }
                );

            // On may not be called multiple times for the same tronsition
            AssertHelper.Throws<ArgumentException>
                (() =>
                {
                    var test = new Workflow<string, string>(state)
                    .Configure(state);

                    test.On(trigger, "state2");
                    test.On("someTrigger");
                }
                );

            // Permit reentry
            AssertHelper.DoesNotThrow(
                () =>
                {
                    var test = new Workflow<string, string>(state)
                    .Configure(state);
                                        
                    test.On(trigger, state);                    
                }
                );

            // On may not be called multiple times for the same tronsition
            AssertHelper.Throws<ArgumentException>
                (() =>
                {
                    var test = new Workflow<string, string>(state)
                    .Configure(state);

                    test.AutoFire(trigger);
                    test.On(trigger, "state2");
                }
                );

        }

        
        [TestMethod]        
        public void WorkflowInit()
        {
            AssertHelper.Throws<ArgumentException>(() => new Workflow<string, string>(null));
            AssertHelper.DoesNotThrow(() => new Workflow<string, string>(string.Empty));
            AssertHelper.DoesNotThrow(() => new Workflow<int, string>(0));

        }

        [TestMethod]
        public void WorkflowTriggersTest()
        {
            var state1 = "state1";
            var state2 = "state2";
            var state3 = "state3";
            var trigger1 = "trigger1";
            var trigger2 = "trigger2";
            var trigger3 = "trigger3";


            Func<string> action1 = () => state1;
            Func<string> action2 = () => state2;

            // 3 triggers 3 states
            // s1 -> { (t1, a1-> [(42, s3), (101, s1), (true, s2)] ),
            //         (t2, a2-> [(1, s1), (2, s2)] }
            // s2 -> { (t3, a-> [("testRetVal", s2), (true, s1)]) }
            Func<Workflow<string, string>> getWf = () =>
            {
                var twf = new Workflow<string, string>(state1);
                twf.Configure(state1)
                    .On(trigger1)
                        .Do(() => state2)
                            .Accepts(state3)
                            .Accepts(state1)
                            .Accepts(state2)
                    .On(trigger2)
                        .Do(action2)
                            .Accepts(state1)
                            .Accepts(state2);
                twf.Configure(state2)
                    .On(trigger3)
                        .Do(() => "testRetVal")
                            .Accepts("testRetVal")
                            .Accepts(state1);

                return twf;
            };

            var wf = getWf();

            Assert.AreEqual(wf.Triggers.Count(), 2);
            Assert.IsTrue(wf.Triggers.Any(item => item == trigger1));
            Assert.IsTrue(wf.Triggers.Any(item => item == trigger2));
            Assert.IsFalse(wf.Triggers.Any(item => item == trigger3));

            wf.Fire(trigger1);
            Assert.AreEqual(wf.Status, state2);
            Assert.AreEqual(wf.Triggers.Count(), 1);
            Assert.IsTrue(wf.Triggers.Any(item => item == trigger3));
        }
        
        [TestMethod]        
        public void WorkflowStaticTransitions()
        {
            var state1 = "state1";
            var state2 = "state2";
            var state3 = "state3";
            var trigger1 = "trigger1";
            var trigger2 = "trigger2";
            var trigger3 = "trigger3";

            var wf = new Workflow<string, string>(state1);

            // static reentrant states are permited
            AssertHelper.DoesNotThrow(
                () => 
                {
                    Assert.AreEqual(state1, wf.Status);
                    wf.Configure(state1).On(trigger1, state1);
                    wf.Fire(trigger1);
                    Assert.AreEqual(state1, wf.Status);
                }
            );

            // simple one transition 2 status test
            wf = new Workflow<string, string>(state1);
            wf.Configure(state1).On(trigger1, state2);
            Assert.AreEqual(state1, wf.Status);
            Assert.AreEqual(wf.PriorStatus, wf.Status);
            wf.Fire(trigger1);
            Assert.AreEqual(state2, wf.Status);
            Assert.AreEqual(wf.PriorStatus, state1);

            // 3 triggers 3 states
            // s1 -> { (t1-> s2), (t2 -> s3) }
            // s2 -> { (t3-> s1) }
            Func<Workflow<string, string>> getWf = () =>
            {
                var twf = new Workflow<string, string>(state1);
                twf.Configure(state1)
                    .On(trigger1, state2)
                    .On(trigger2, state3);
                twf.Configure(state2)
                    .On(trigger3, state1);

                    return twf;
            };

            wf = getWf();
            wf.Fire(trigger1);
            // test state 2
            Assert.AreEqual(wf.PriorStatus, state1);
            Assert.AreEqual(state2, wf.Status);
            AssertHelper.Throws<InvalidOperationException>(() => wf.Fire(trigger1));
            AssertHelper.Throws<InvalidOperationException>(() => wf.Fire(trigger2));

            wf.Fire(trigger3);
            Assert.AreEqual(state2, wf.PriorStatus);
            Assert.AreEqual(state1, wf.Status);

            wf.Fire(trigger2);
            Assert.AreEqual(state1, wf.PriorStatus);
            Assert.AreEqual(state3, wf.Status);

        }
                
        [TestMethod]        
        public void WorkflowDynamicTransitions()
        {
            var state1 = "state1";
            var state2 = "state2";
            var state3 = "state3";
            var trigger1 = "trigger1";
            var trigger2 = "trigger2";
            var trigger3 = "trigger3";
            
            var actionFired = false;
            var actionFired2 = false;

            Func<string> action1 = () => { actionFired = true; return state1; };
            Func<string> action2 = () => { actionFired2 = true; return state2; };
                        

            // if transitioning to an unaccepted state throws error
            var wf = new Workflow<string, string>(state1);
            AssertHelper.Throws<ArgumentException>(
                () =>
                {
                    wf.Configure(state1)
                        .On(trigger1)
                            .Do(action1)
                                .Accepts(state3)
                                .Accepts(state2);
                    
                    wf.Fire(trigger1);
                }
            );

            // undefined trigger exception
            wf = new Workflow<string, string>(state1);
            AssertHelper.Throws<InvalidOperationException>(() => wf.Fire(trigger1));

            actionFired = false;
            wf = new Workflow<string, string>(state1);

            // simple self transition
            // dynamic transitions allow for reentry
            AssertHelper.DoesNotThrow(
                () => wf.Configure(state1)
                        .On(trigger1)
                            .Do(action1)
                                .Accepts(state1)
            );
            Assert.AreEqual(state1, wf.Status);
            Assert.AreEqual(state1, wf.PriorStatus);
            Assert.IsFalse(actionFired);
            wf.Fire(trigger1);
            Assert.AreEqual(state1, wf.Status);
            Assert.AreEqual(state1, wf.PriorStatus);
            Assert.IsTrue(actionFired);
            

            // 3 triggers 3 states
            // s1 -> { (t1, a1-> [(42, s3), (101, s1), (true, s2)] ),
            //         (t2, a2-> [(1, s1), (2, s2)] }
            // s2 -> { (t3, a-> [("testRetVal", s2), (true, s1)]) }
            Func<Workflow<string, string>> getWf = () =>
            {
                var twf = new Workflow<string, string>(state1);
                twf.Configure(state1)
                    .On(trigger1)
                        .Do(()=> state2)
                            .Accepts(state3)
                            .Accepts(state1)
                            .Accepts(state2)
                    .On(trigger2)
                        .Do(action2)
                            .Accepts(state1)
                            .Accepts(state2);
                twf.Configure(state2)
                    .On(trigger3)
                        .Do(() => "testRetVal")
                            .Accepts("testRetVal")
                            .Accepts(state1);

                return twf;
            };
                        
            wf = getWf();
                        
            Assert.AreEqual(state1, wf.Status);
            wf.Fire(trigger2);
            Assert.AreEqual(state1, wf.PriorStatus);
            Assert.AreEqual(state2, wf.Status);
            wf.Fire(trigger3);
            Assert.AreEqual(state2, wf.PriorStatus);
            Assert.AreEqual("testRetVal", wf.Status);
                        
            wf = getWf();
            wf.Fire(trigger2);
            Assert.AreEqual(state1, wf.PriorStatus);
            Assert.AreEqual(state2, wf.Status);
         
        }
                
        [TestMethod]        
        public void WorkflowAutoTransitions()
        {
            var state1 = "state1";
            var state2 = "state2";
            var state3 = "state3";
            var trigger1 = "trigger1";
            var trigger2 = "trigger2";            

            var actionFired = false;
                        
            Func<string> action1 = () => { actionFired = true; return state1; };
            Func<string> action2 = () => state2;

            // no if condition that matches action result (with no default specified)
            // will throw an exception (trigger is not handeled)
            var wf = new Workflow<string, string>(state1);
            AssertHelper.Throws<ArgumentException>(
                () =>
                {
                    wf.Configure(state1)
                        .AutoFire(trigger1)
                            .Do(action1)
                                .Accepts("not found")
                                .Accepts(state2);

                    wf.Fire(trigger1);
                }
            );
            
            // may not have multiple auto fire triggers
            wf = new Workflow<string, string>(state1);
            AssertHelper.Throws<ArgumentException>(
                () =>

                    wf.Configure(state1)
                        .AutoFire(trigger1)
                            .Do(action1)
                                .Accepts(state2)
                        .AutoFire(trigger2)
                            .Do(action2)
                                .Accepts(state2)
               
            );
            
            Func<Workflow<string, string>> getWf = () =>
            {
                var wftmp = new Workflow<string, string>(state1);

                wftmp.Configure(state1)
                    .On(trigger1, state2);
                wftmp.Configure(state2)
                    .AutoFire(trigger2)
                        .Do(() => { actionFired = true; return state3; })
                            .Accepts(state3);
                        

                return wftmp;
            };

            // test auto transition
            actionFired = false;
            Assert.IsFalse(actionFired);
            wf = getWf();
            wf.Fire(trigger1);
            Assert.IsTrue(actionFired);
            Assert.AreEqual(state2, wf.PriorStatus);
            Assert.AreEqual(state3, wf.Status);
            /*   */
        }

        /*
        private Func<A, R> MakeFunction<A, R>(Func<A, R> f, A a)
        {
            return f; 
        }

        [TestMethod]
        public void TestGarbage()
        {
            var f = MakeFunction(c=>c.Name, new {Name="", Age=0, Address=""} );
        }
         */
    }
}
