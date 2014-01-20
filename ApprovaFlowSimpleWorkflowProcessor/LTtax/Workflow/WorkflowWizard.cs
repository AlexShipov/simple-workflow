using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTtax.Workflow;
using LTtax.Enums;

namespace LTtax.Workflow
{
    public abstract class WorkflowWizard
    {
        public class Model
        {
            /// <summary>
            /// Trigger that is to be executed on the Status.
            /// </summary>
            public string Trigger { get; set; }

            /// <summary>
            /// Current Status.
            /// </summary>
            public string Status { get; set; }

            /// <summary>
            /// List of all available triggers.
            /// </summary>
            public IEnumerable<string> Triggers { get; set; }

            /// <summary>
            /// Status prior to current.
            /// </summary>
            public string PriorStatus { get; set; }            
        }

        private Workflow<StringEnumBase, StringEnumBase> InitWorkflow(Model model)
        {
            var wf = new Workflow<StringEnumBase, StringEnumBase>((StringEnumBase)model.Status);
            InitWorkflow(wf);
            return wf;
        }

        protected abstract void InitWorkflow(Workflow<StringEnumBase, StringEnumBase> wf);        

        protected Model DoAction(Model model)
        {
            var wf = InitWorkflow(model);
            wf.Fire((StringEnumBase)model.Trigger);
            model.PriorStatus = wf.PriorStatus;
            model.Status = wf.Status;
            model.Triggers = wf.Triggers.Select(item => (string)item);
            return model;
        }

    }
}
