using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LTtax.Workflow;

namespace ApprovaFlowSimpleWorkflowProcessor.LTtax
{
    public class WHPayrollImportRecordWF
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

        public Status CurrentStatus
        {
            get { return m_wf.Status; }
        }

        public void Fire(Trigger trigger)
        {
            m_wf.Fire(trigger);
        }

        protected Workflow<Status, Trigger> m_wf;

        protected Workflow<Status, Trigger> InitWF()
        {
            return DoInitWF(Status.Init);
        }

        public Status Delete { get; set; }
        public Status MoveRawToDe { get; set; }
        public Status CheckExistingDe { get; set; }
        public Status ValidateRaw { get; set; }
        public Status UploadRaw { get; set; }
        public Status CheckForRaw { get; set; }
        public Status CheckForDuplicate { get; set; }
        public Status CheckFileError { get; set; }
        
        private  Workflow<Status, Trigger> DoInitWF(Status status)
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


            return wf;
        }

        public WHPayrollImportRecordWF(
            Status deleteStatus, 
            Status moveRawToDe,
            Status checkExistingDeStatus,
            Status validateRawStatus,
            Status uploadRawStatus,
            Status checkForRawStatus,
            Status checkForDuplicateStatus,
            Status checkFileErrorStatus)
        {
            Delete = deleteStatus;
            MoveRawToDe = moveRawToDe;
            CheckExistingDe = checkExistingDeStatus;
            ValidateRaw = validateRawStatus;
            UploadRaw = uploadRawStatus;
            CheckForRaw = checkForRawStatus;
            CheckForDuplicate = checkForDuplicateStatus;
            CheckFileError = checkFileErrorStatus;
            m_wf = this.InitWF();
        }

        private Status DeleteRawData()
        {
            return this.Delete;
        }

        private Status MoveRawToDE()
        {
            return this.MoveRawToDe;
        }

        private Status CheckExistingDataEntry()
        {
            return this.CheckExistingDe;
        }

        private Status ValidateRawData()
        {
            return this.ValidateRaw;
        }

        private Status UploadRawData()
        {
            return this.UploadRaw;
        }

        private Status CheckForRawData()
        {
            return this.CheckForRaw;
        }

        private Status CheckForDuplicates()
        {
            return this.CheckForDuplicate;
        }

        private Status CheckFileErrors()
        {
            return this.CheckFileError;
        }
    }
}
