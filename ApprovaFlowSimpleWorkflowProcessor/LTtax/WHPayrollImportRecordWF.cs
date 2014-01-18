using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LTtax.Workflow;

namespace ApprovaFlowSimpleWorkflowProcessor.LTtax
{
    class WHPayrollImportRecordWF
    {
        public enum Status
        {
            Init = 0,
            ImportStarted = 1,
            ReadyToImport = 2,
            ConfirmContinue = 16,
            Validated = 3,
            ConfirmAppendReplaceCancel = 4,
            ReadyToUpload = 5,
            Uploaded = 6,
            RawValid = 7,
            HasDataEntry = 8,
            ReadyToMoveDE = 9,
            RawNotValid = 10,
            ImportCanceled = 11,
            ImportFailed = 12,
            ImportSucceeded = 13,
            ImportSucceededWithErrors = 14,
            ImportSucceededHeldInWhImported = 15
        }

        public enum Trigger
        {
            StartImport = 0,
            CheckFileErrors = 12,
            CheckForDuplicates = 1,
            Cancel = 2,
            Continue = 3,
            CheckForRaw = 4,
            DoUpload = 5,
            ValidateUploaded = 6,
            CheckForDE = 7,
            Append = 8,
            MoveRawToDE = 9,
            ContinueAfterRawReports = 10,
            Replace = 11
        }

        protected Workflow<Status, Trigger> m_wf;

        protected Workflow<Status, Trigger> InitWF()
        {
            return InitWF(Status.Init);
        }

        protected Workflow<Status, Trigger> InitWF(Status status)
        {
            var wf = new Workflow<Status, Trigger>(status);

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
                        .Accepts(Status.ReadyToImport)
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
                .On(Trigger.ContinueAfterRawReports, Status.RawValid)
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


            return wf;
        }

        private Status DeleteRawData()
        {
            throw new NotImplementedException();
        }

        private Status MoveRawToDE()
        {
            throw new NotImplementedException();
        }

        private Status CheckExistingDataEntry()
        {
            throw new NotImplementedException();
        }

        private Status ValidateRawData()
        {
            throw new NotImplementedException();
        }

        private Status UploadRawData()
        {
            throw new NotImplementedException();
        }

        private Status CheckForRawData()
        {
            throw new NotImplementedException();
        }

        protected Status CheckForDuplicates()
        {
            throw new NotImplementedException();
        }

        protected Status CheckFileErrors()
        {
            try
            {
                return Status.ReadyToImport;
                // entities provider
                // check file for errors
            }
            catch(Exception ex)
            {
                return Status.ImportFailed;
                // log provider
                // test
            }
        }
    }
}
