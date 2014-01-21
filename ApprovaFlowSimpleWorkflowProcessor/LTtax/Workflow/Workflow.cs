namespace LTtax.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Stateless;
    using LTtax.Utils;

    public interface IWorkflow<TStatus, TTrigger>
    {
        ITransition<TStatus, TTrigger> Configure(TStatus status);
        TStatus Status { get; }
        TStatus PriorStatus { get; }
        IEnumerable<TTrigger> Triggers { get; }
        IWorkflow<TStatus, TTrigger> Fire(TTrigger trigger);
    }

    public interface ITransitionSelection<TStatus, TTrigger>
    {
        ITransitionSelection<TStatus, TTrigger> Accepts(TStatus status);
        ITransition<TStatus, TTrigger> On(TTrigger trigger, TStatus status);
        ITransitionAction<TStatus, TTrigger> On(TTrigger trigger);
        ITransitionAction<TStatus, TTrigger> AutoFire(TTrigger trigger);
    }

    public interface ITransitionAction<TStatus, TTrigger>
    {
        ITransitionSelection<TStatus, TTrigger> Do(Func<TStatus> action);
    }

    public interface ITransition<TStatus, TTrigger>
    {
        ITransitionAction<TStatus, TTrigger> AutoFire(TTrigger trigger);
        ITransitionAction<TStatus, TTrigger> On(TTrigger trigger);
        ITransition<TStatus, TTrigger> On(TTrigger trigger, TStatus status);
        bool IsAutoFire { get; set; }
    }


    /// <summary>
    /// Simple workflow class implemented as a state machine.
    /// </summary>
    /// <typeparam name="TStatus"></typeparam>
    /// <typeparam name="TTrigger"></typeparam>
    public class Workflow<TStatus, TTrigger> : IWorkflow<TStatus, TTrigger>
    {
        protected StateMachine<TStatus, TTrigger> m_machine;        // handles transition and state logic
        protected Dictionary<TStatus, HashSet<Transition>> m_transitionMap; //map of available transitions for a status
        protected TStatus m_prior; // prior status
        
        /// <summary>
        /// Registers transitions as static (internal use only).
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="fromStatus"></param>
        /// <param name="toStatus"></param>
        /// <returns></returns>
        protected Workflow<TStatus, TTrigger> RegisterStaticTransition(Transition transition, TStatus fromStatus, TStatus toStatus)
        {
            if (fromStatus.Equals(toStatus))
            {
                m_machine.Configure(fromStatus).PermitReentry(transition.Trigger);
            }
            else
            {
                m_machine.Configure(fromStatus).Permit(transition.Trigger, toStatus);
            }
            return this;
        }

        /// <summary>
        /// Registers a dynamic transition (internal use only).
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        protected Workflow<TStatus, TTrigger> RegisterDynamicTransition(Transition transition, TStatus status)
        {
            Enforce.That(!transition.IsAutoFire || !m_transitionMap[status].Any(item => item.IsAutoFire), "Status may only contain one auto fire transition.");
            
            m_transitionMap[status].Add(transition);
            m_machine.Configure(status).PermitDynamic(transition.Trigger, transition.TransitionHandler);
            return this;
        }

        /// <summary>
        /// Creates new workflow object and sets its initial status.
        /// </summary>
        /// <param name="initialStatus">Initial status.</param>
        public Workflow(TStatus initialStatus)
        {
            if (typeof(TStatus).IsSubclassOf(typeof(object)))
            {
                Enforce.That(initialStatus != null, "Initial status may not be null.");
            }
            
            m_transitionMap = new Dictionary<TStatus, HashSet<Transition>>();
            m_machine = new StateMachine<TStatus, TTrigger>(initialStatus);
            m_prior = m_machine.State;
        }

        /// <summary>
        /// Configures a transition for a given status.
        /// </summary>
        /// <param name="status">Status being configured.</param>
        /// <returns>Transition object that is used to configure the thransition.</returns>
        public ITransition<TStatus, TTrigger> Configure(TStatus status)
        {
            if (!m_transitionMap.ContainsKey(status))
            {
                m_transitionMap.Add(status, new HashSet<Transition>());
            }
            return new Transition(status, this);
        }

        /// <summary>
        /// Gets prior status.
        /// </summary>
        public TStatus PriorStatus
        {
            get
            {
                return m_prior;
            }
        }

        /// <summary>
        /// Gets current status. 
        /// </summary>
        public TStatus Status
        {
            get
            {
                return m_machine.State;
            }
        }

        /// <summary>
        /// Gets valid triggers for current status.
        /// </summary>
        public IEnumerable<TTrigger> Triggers
        {
            get
            {
                return m_machine.PermittedTriggers;
            }
        }

        /// <summary>
        /// Fires trigger on current status.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public IWorkflow<TStatus, TTrigger> Fire(TTrigger trigger)
        {
            m_prior = m_machine.State;
            m_machine.Fire(trigger);
            
            // if current state (after trigger fire) has transitions
            HashSet<Transition> transitions;
            if (m_transitionMap.TryGetValue(m_machine.State, out transitions) )
            {
                var autoTransition = transitions.Where(item => item.IsAutoFire).FirstOrDefault();
                if(autoTransition != null)
                {
                    this.Fire(autoTransition.Trigger);
                }
            }

            return this;
        }


        public class Transition : ITransition<TStatus, TTrigger>
        {
            protected Workflow<TStatus, TTrigger> m_owner;
            protected TStatus m_status;
            protected TransitionAction m_action;

            public bool IsAutoFire { get; set; }

            protected TTrigger m_trigger;

            protected TStatus CurrentStatus { get { return m_status; } }

            /// <summary>
            /// Registers transition.
            /// </summary>
            protected void ReisterTransition()
            {
                m_owner.RegisterDynamicTransition(this, m_status);
            }

            /// <summary>
            /// Creates new transition for current status.
            /// </summary>
            /// <returns></returns>
            protected Transition NewTransition()
            {
                return new Transition(m_status, m_owner);
            }

            protected void DoInit(TTrigger trigger)
            {
                Enforce.That(m_action == null, "Action may only be set once on a Transition.");

                if (typeof(TTrigger).IsSubclassOf(typeof(object)))
                {
                    Enforce.That(trigger != null, "Trigger may not be null.");
                }

                m_action = new TransitionAction(this);
                m_trigger = trigger;
            }            

            internal Transition(TStatus status, Workflow<TStatus, TTrigger> owner)
            {
                this.IsAutoFire = false;
                m_status = status;
                m_owner = owner;
                m_action = null;
            }

            /// <summary>
            /// Gets transition handler.
            /// </summary>
            internal Func<TStatus> TransitionHandler
            {
                get { return m_action.HandleTransition; }
            }

            /// <summary>
            /// Gets transition trigger.
            /// </summary>
            internal TTrigger Trigger
            {
                get { return m_trigger; }
            }

            /// <summary>
            /// Creates an auto fire transition. A Status is automatically transitioned (its trigger is fired)
            /// </summary>
            /// <param name="trigger">Trigger to be fired automatically.</param>
            /// <returns>Transition action object.</returns>
            public ITransitionAction<TStatus, TTrigger> AutoFire(TTrigger trigger)
            {
                this.IsAutoFire = true;
                return this.On(trigger);
            }

            /// <summary>
            /// On given trigger transition to states depending on transition action.
            /// </summary>
            /// <param name="trigger">Transition trigger.</param>
            /// <returns>Transition action object.</returns>
            public ITransitionAction<TStatus, TTrigger> On(TTrigger trigger)
            {
                this.DoInit(trigger);
                return m_action;
            }

            /// <summary>
            /// Single status transition.
            /// </summary>
            /// <param name="trigger">Transition Trigger.</param>
            /// <param name="status">Status after transition.</param>
            public ITransition<TStatus, TTrigger> On(TTrigger trigger, TStatus status)
            {
                if (typeof(TStatus).IsSubclassOf(typeof(object)))
                {
                    Enforce.That(status != null, "Initial status may not be null.");
                }
                
                this.DoInit(trigger);
                m_owner.RegisterStaticTransition(this, m_status, status);

                return this.NewTransition();
            }

            public class TransitionAction : ITransitionAction<TStatus, TTrigger>
            {
                protected Func<TStatus> m_action;
                private Func<TStatus> m_tmpAction;
                protected TransitionSelection m_selection;
                protected Transition m_owner;

                protected void ReisterTransition()
                {
                    // set action  variable indicating that the transition is registered
                    m_action = m_tmpAction;
                    m_owner.ReisterTransition();
                }                

                internal TransitionAction(Transition owner)
                {
                    m_owner = owner;
                    m_action = null;
                }

                /// <summary>
                /// Execute transition action.
                /// </summary>
                /// <returns>Next status.</returns>
                internal TStatus HandleTransition()
                {
                    return m_selection.Transition(m_action(), m_owner.CurrentStatus);
                }

                /// <summary>
                /// Set transition action.
                /// </summary>
                /// <param name="action">Action performed when trigger is fired to determine next status.</param>
                /// <returns></returns>
                public ITransitionSelection<TStatus, TTrigger> Do(Func<TStatus> action)
                {
                    Enforce.That(m_action == null, "Action may not be reinitialized.");
                    Enforce.ArgumentNotNull(action, "Null transition action.");

                    m_tmpAction = action;
                    m_selection = new TransitionSelection(this);
                    return m_selection;
                }

                protected Transition NewTransition()
                {
                    return m_owner.NewTransition();
                }                

                /// <summary>
                /// Handles status transitions based on action result.
                /// </summary>
                public class TransitionSelection : ITransitionSelection<TStatus, TTrigger>
                {
                    protected HashSet<TStatus> m_transitionMap;
                    protected bool m_isRegistered;
                    protected TransitionAction m_owner;

                    /// <summary>
                    /// Creates new transition used used in fluent helper methods On and AutoFire
                    /// To continue status config chain
                    /// </summary>
                    /// <returns></returns>
                    protected Transition NewTransition()
                    {
                        return m_owner.NewTransition();
                    }

                    /// <summary>
                    /// Insure that the transition is registered.
                    /// This indicates that the transition chain is complete and may be registered.
                    /// </summary>
                    protected void ReisterTransition()
                    {
                        if (!m_isRegistered)
                        {
                            m_isRegistered = true;
                            m_owner.ReisterTransition();
                        }
                    }

                    /// <summary>
                    /// Checks if action result is an accepted status for current transition.
                    /// </summary>
                    /// <param name="actionResult">Action result.</param>
                    /// <returns>Transition status.</returns>
                    internal TStatus Transition(TStatus actionResult, TStatus curreStatus)
                    {
                        Enforce.That(m_transitionMap.Contains(actionResult),
                            string.Format("Unhandled transition exception {0} in status {1}.", actionResult.ToString(), curreStatus.ToString()));

                        var nextState = actionResult;

                        return nextState;

                    }

                    internal TransitionSelection(TransitionAction owner)
                    {
                        m_owner = owner;
                        m_isRegistered = false;                        
                        m_transitionMap = new HashSet<TStatus>();
                    }

                    /// <summary>
                    /// Add status to accepted statuses.
                    /// </summary>                    
                    /// <param name="status">Accepted status.</param>
                    /// <returns></returns>
                    public ITransitionSelection<TStatus, TTrigger> Accepts(TStatus status)
                    {
                        m_transitionMap.Add(status);
                        this.ReisterTransition();

                        return this;
                    }

                    #region Fluent extensions
                    public ITransition<TStatus, TTrigger> On(TTrigger trigger, TStatus status)
                    {
                        return this.NewTransition().On(trigger, status);
                    }

                    public ITransitionAction<TStatus, TTrigger> On(TTrigger trigger)
                    {
                        return this.NewTransition().On(trigger);
                    }

                    public ITransitionAction<TStatus, TTrigger> AutoFire(TTrigger trigger)
                    {
                        return this.NewTransition().AutoFire(trigger);
                    }
                    #endregion
                }
            }
        }
    }
}
