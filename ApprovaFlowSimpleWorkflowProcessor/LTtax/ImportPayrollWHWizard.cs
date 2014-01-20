using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTtax.Workflow;
using LTtax.Enums;

namespace ApprovaFlowSimpleWorkflowProcessor.LTtax
{
    public class ImportPayrollWHWizard : WorkflowWizard
    {
        public StringEnumBase MoveRawToDEStatus = Status.ImportSucceeded;
        public StringEnumBase CheckExistingDataEntryStatus = Status.ReadyToMoveDE;
        public StringEnumBase ValidateRawDataStatus = Status.RawValid;
        public StringEnumBase UploadRawDataStatus = Status.Uploaded;
        public StringEnumBase DeleteRawDataStatus = Status.ReadyToUpload;
        public StringEnumBase CheckForRawDataStatus = Status.ReadyToUpload;
        public StringEnumBase CheckForDuplicatesStatus = Status.Validated;
        public StringEnumBase CheckFileErrorsStatus = Status.ReadyToImport;

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
            public static readonly StringEnumBase Continue = new Trigger("Continue");
            public static readonly StringEnumBase Replace = new Trigger("Replace");
            public static readonly StringEnumBase Append = new Trigger("Append");
            public static readonly StringEnumBase CheckForRaw = new Trigger("CheckForRaw");
            public static readonly StringEnumBase DoUpload = new Trigger("DoUpload");
            public static readonly StringEnumBase ValidateUploaded = new Trigger("ValidateUploaded");
            public static readonly StringEnumBase ContinueAfterRawReports = new Trigger("ContinueAfterRawReports");
            public static readonly StringEnumBase CheckForDE = new Trigger("CheckForDE");
            public static readonly StringEnumBase MoveRawToDE = new Trigger("MoveRawToDE");

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
            public static readonly StringEnumBase ConfirmContinue = new Status("ConfirmContinue");
            public static readonly StringEnumBase Validated = new Status("Validated");
            public static readonly StringEnumBase ConfirmAppendReplaceCancel = new Status("ConfirmAppendReplaceCancel");            
            public static readonly StringEnumBase ReadyToUpload = new Status("ReadyToUpload");
            public static readonly StringEnumBase Uploaded = new Status("Uploaded");
            public static readonly StringEnumBase RawNotValid = new Status("RawNotValid");
            public static readonly StringEnumBase RawValid = new Status("RawValid");
            public static readonly StringEnumBase HasDataEntry = new Status("HasDataEntry");
            public static readonly StringEnumBase ReadyToMoveDE = new Status("ReadyToMoveDE");
            public static readonly StringEnumBase ImportFailed = new Status("ImportFailed");
            public static readonly StringEnumBase ImportCanceled = new Status("ImportCanceled");
            public static readonly StringEnumBase ImportSucceededWithErrors = new Status("ImportSucceededWithErrors");
            public static readonly StringEnumBase ImportSucceededHeldInWhImported = new Status("ImportSucceededHeldInWhImported");
            public static readonly StringEnumBase ImportSucceeded = new Status("ImportSucceeded");
        }
        
        /// <summary>
        /// Initializes the import whithholding worflow in a given status.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected override void InitWorkflow(Workflow<StringEnumBase, StringEnumBase> wf)
        {
            wf.Configure(Status.Init)
                .On(Trigger.StartImport, Status.ImportStarted);

            wf.Configure(Status.ImportStarted)
                .AutoFire(Trigger.CheckFileErrors)
                    .Do(this.CheckFileErrors)
                        .Accepts(Status.ReadyToImport)
                        .Accepts(Status.ImportFailed);

            wf.Configure(Status.ReadyToImport)
                .AutoFire(Trigger.CheckForDuplicates)
                    .Do(this.CheckForDuplicates)
                        .Accepts(Status.Validated)
                        .Accepts(Status.ConfirmContinue)
                        .Accepts(Status.ImportFailed);

            wf.Configure(Status.ConfirmContinue)
                .On(Trigger.Cancel, Status.ImportCanceled)
                .On(Trigger.Continue, Status.Validated);

            wf.Configure(Status.Validated)
                .AutoFire(Trigger.CheckForRaw)
                    .Do(this.CheckForRawData)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.ReadyToUpload)
                        .Accepts(Status.ConfirmAppendReplaceCancel);

            wf.Configure(Status.ConfirmAppendReplaceCancel)
                            .On(Trigger.Cancel, Status.ImportCanceled)
                            .On(Trigger.Replace)
                                .Do(this.DeleteRawData)
                                    .Accepts(Status.ImportFailed)
                                    .Accepts(Status.ReadyToUpload)
                            .On(Trigger.Append, Status.ReadyToUpload);

            wf.Configure(Status.ReadyToUpload)
                .AutoFire(Trigger.DoUpload)
                    .Do(this.UploadRawData)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.Uploaded);

            wf.Configure(Status.Uploaded)
                .AutoFire(Trigger.ValidateUploaded)
                    .Do(this.ValidateRawData)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.RawValid)
                        .Accepts(Status.RawNotValid);

            wf.Configure(Status.RawNotValid)
                .On(Trigger.ContinueAfterRawReports, Status.ImportSucceededWithErrors);

            wf.Configure(Status.RawValid)
                .On(Trigger.CheckForDE)
                    .Do(this.CheckExistingDataEntry)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.HasDataEntry)
                        .Accepts(Status.ReadyToMoveDE);

            wf.Configure(Status.HasDataEntry)
                .On(Trigger.Append, Status.ReadyToMoveDE)
                .On(Trigger.Cancel, Status.ImportSucceededHeldInWhImported);

            wf.Configure(Status.ReadyToMoveDE)
                .AutoFire(Trigger.MoveRawToDE)
                    .Do(this.MoveRawToDE)
                        .Accepts(Status.ImportFailed)
                        .Accepts(Status.ImportSucceeded);
        }

        private StringEnumBase MoveRawToDE()
        {
            return MoveRawToDEStatus;
        }

        private StringEnumBase CheckExistingDataEntry()
        {
            return CheckExistingDataEntryStatus;
        }

        private StringEnumBase ValidateRawData()
        {
            return ValidateRawDataStatus;
        }

        private StringEnumBase UploadRawData()
        {
            return UploadRawDataStatus;
        }

        private StringEnumBase DeleteRawData()
        {
            return DeleteRawDataStatus;
        }

        private StringEnumBase CheckForRawData()
        {
            return CheckForRawDataStatus;
        }

        private StringEnumBase CheckForDuplicates()
        {
            return this.CheckForDuplicatesStatus;
        }

        private StringEnumBase CheckFileErrors()
        {
            return this.CheckFileErrorsStatus;
        }
    }
}
