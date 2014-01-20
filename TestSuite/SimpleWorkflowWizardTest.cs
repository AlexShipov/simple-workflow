using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using LTtax.Workflow;
using LTtax.Enums;
using ApprovaFlowSimpleWorkflowProcessor.LTtax;

namespace TestSuite
{
    [TestClass]
    public class SimpleWorkflowWizardTest
    {
        [TestMethod]
        public void TestAllSuccess()
        {
            var simpleWizard = new SimpleWizardTest();
            var model = simpleWizard.NewModel();
            model.Trigger = SimpleWizardTest.Trigger.StartImport;

            Assert.AreEqual((SimpleWizardTest.Status)model.Status, SimpleWizardTest.Status.Init);
            Assert.AreEqual(model.Status, (string)SimpleWizardTest.Status.Init);
            simpleWizard.Action(model);
            Assert.AreEqual(model.PriorStatus, (string)SimpleWizardTest.Status.Validated);
            Assert.AreEqual(model.Status, (string)SimpleWizardTest.Status.ImportSucceeded);
        }

        [TestMethod]
        public void ImportCancelTest()
        {
            // simulate import cancel
            var simpleWizard = new SimpleWizardTest()
            {
                ValidateExistingDataStatus = SimpleWizardTest.Status.ConfirmContinue
            };
            var model = simpleWizard.NewModel();

            model.Trigger = SimpleWizardTest.Trigger.StartImport;            
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ConfirmContinue, model.Status);
            Assert.AreEqual((string)SimpleWizardTest.Status.ReadyToImport, model.PriorStatus);
            model.Trigger = SimpleWizardTest.Trigger.Cancel;
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ImportCanceled, model.Status);
            Assert.AreEqual((string)SimpleWizardTest.Status.ConfirmContinue, model.PriorStatus);
        }

        [TestMethod]
        public void ImportFailTest()
        {
            // simulate failure on CheckFileForErrors action
            var simpleWizard = new SimpleWizardTest()
            {
                CheckFileForErrorsStatus = SimpleWizardTest.Status.ImportFailed
            };
            var model = simpleWizard.NewModel();

            model.Trigger = SimpleWizardTest.Trigger.StartImport;
            Assert.AreEqual((SimpleWizardTest.Status)model.Status, SimpleWizardTest.Status.Init);            
            simpleWizard.Action(model);
            Assert.AreEqual(model.Status, (string)SimpleWizardTest.Status.ImportFailed);


            // simulate failure on CheckFileForErrors action
            simpleWizard = new SimpleWizardTest()
            {
                ValidateExistingDataStatus = SimpleWizardTest.Status.ImportFailed
            };
            model = simpleWizard.NewModel();

            model.Trigger = SimpleWizardTest.Trigger.StartImport;
            Assert.AreEqual((SimpleWizardTest.Status)model.Status, SimpleWizardTest.Status.Init);
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ReadyToImport, model.PriorStatus);
            Assert.AreEqual(model.Status, (string)SimpleWizardTest.Status.ImportFailed);

            // simulate failure in Replace trigger on ConfirmContinue status
            // indicate existamce of prior data requiring trigger selection
            // and an error on DeleteExisting action
            simpleWizard = new SimpleWizardTest()
            {
                ValidateExistingDataStatus = SimpleWizardTest.Status.ConfirmContinue,
                DeleteExistingStatus = SimpleWizardTest.Status.ImportFailed
            };
            model = simpleWizard.NewModel();

            model.Trigger = SimpleWizardTest.Trigger.StartImport;            
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ConfirmContinue, model.Status);
            Assert.AreEqual((string)SimpleWizardTest.Status.ReadyToImport, model.PriorStatus);

            model.Trigger = SimpleWizardTest.Trigger.Replace;
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ConfirmContinue, model.PriorStatus);
            Assert.AreEqual(model.Status, (string)SimpleWizardTest.Status.ImportFailed);

            // simulate failure on DoImportAction
            simpleWizard = new SimpleWizardTest()
            {                
                DoImportStatus = SimpleWizardTest.Status.ImportFailed
            };
            model = simpleWizard.NewModel();

            model.Trigger = SimpleWizardTest.Trigger.StartImport;
            simpleWizard.Action(model);
            Assert.AreEqual((string)SimpleWizardTest.Status.ImportFailed, model.Status);
            Assert.AreEqual((string)SimpleWizardTest.Status.Validated, model.PriorStatus);
        }
    }
}
