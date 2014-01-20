using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTtax.Workflow;
using LTtax.Enums;

namespace ApprovaFlowSimpleWorkflowProcessor.LTtax
{
    public class SimpleWizardTest : WorkflowWizard
    {
        public WorkflowWizard.Model NewModel()
        {
            return new WorkflowWizard.Model()
            {
                Status = Status.Init,
                PriorStatus = Status.Init,
                Triggers = new[] { (string)Trigger.StartImport }
            };
        }

        public WorkflowWizard.Model Action(Model model)
        {
            return base.DoAction(model);
        }

        public sealed class Trigger : StringEnumBase
        {
            private Trigger(string value)
                : base(value)
            {
            }

            static Trigger()
            {
                StringEnumBase.InitLookup<Trigger>();
            }

            public static readonly StringEnumBase StartImport = new Trigger("StartImport");
            public static readonly StringEnumBase CheckFileErrors = new Trigger("CheckFileErrors");
            public static readonly StringEnumBase CheckForDuplicates = new Trigger("CheckForDuplicates");
            public static readonly StringEnumBase Cancel = new Trigger("Cancel");            
            public static readonly StringEnumBase Replace = new Trigger("Replace");
            public static readonly StringEnumBase Append = new Trigger("Append");
            public static readonly StringEnumBase DoImport = new Trigger("DoImport");
        }

        public sealed class Status : StringEnumBase
        {
            private Status(string value)
                : base(value)
            {
            }

            static Status()
            {
                StringEnumBase.InitLookup<Status>();
            }

            public static readonly StringEnumBase Init = new Status("Init");
            public static readonly StringEnumBase ImportStarted = new Status("ImportStarted");
            public static readonly StringEnumBase ReadyToImport = new Status("ReadyToImport");
            public static readonly StringEnumBase ValidateExistingData = new Status("ValidateExistingData");
            public static readonly StringEnumBase ConfirmContinue = new Status("ConfirmContinue");
            public static readonly StringEnumBase Validated = new Status("Validated");
            public static readonly StringEnumBase ImportFailed = new Status("ImportFailed");
            public static readonly StringEnumBase ImportCanceled = new Status("ImportCanceled");
            public static readonly StringEnumBase ImportSucceeded = new Status("ImportSucceeded");
            
        }

        protected override void InitWorkflow(Workflow<StringEnumBase, StringEnumBase> wf)
        {
            wf.Configure(Status.Init)
                .On(Trigger.StartImport, Status.ImportStarted); // on initial status transition to import started
                        
            wf.Configure(Status.ImportStarted)
                .AutoFire(Trigger.CheckFileErrors)      // automatically fired when this state is entered via trigger
                    .Do(this.CheckFileForErrors)
                        .Accepts(Status.ImportFailed)   // end status
                        .Accepts(Status.ReadyToImport);

            wf.Configure(Status.ReadyToImport)
                .AutoFire(Status.ValidateExistingData)
                    .Do(this.ValidateExistingData)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.Validated)
                        .Accepts(Status.ConfirmContinue);

            wf.Configure(Status.ConfirmContinue)
                .On(Trigger.Append, Status.Validated)
                .On(Trigger.Cancel, Status.ImportCanceled)
                .On(Trigger.Replace)
                    .Do(this.DeleteExisting)
                        .Accepts(Status.Validated)
                        .Accepts(Status.ImportFailed);

            wf.Configure(Status.Validated)
                .AutoFire(Trigger.DoImport)
                    .Do(this.DoImport)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.ImportSucceeded);
            
        }

        private StringEnumBase DoImport()
        {
            return this.DoImportStatus;
        }

        private StringEnumBase DeleteExisting()
        {
            return this.DeleteExistingStatus;
        }

        private StringEnumBase ValidateExistingData()
        {
            return this.ValidateExistingDataStatus;
        }

        private StringEnumBase CheckFileForErrors()
        {
            return this.CheckFileForErrorsStatus;
        }

        public StringEnumBase DoImportStatus = Status.ImportSucceeded;
        public StringEnumBase DeleteExistingStatus = Status.Validated;
        public StringEnumBase ValidateExistingDataStatus = Status.Validated;
        public StringEnumBase CheckFileForErrorsStatus = Status.ReadyToImport;
    }
}
