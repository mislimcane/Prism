using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Properties;
using System.Threading;

namespace Prism.Commands
{
    /// <summary>
    /// An <see cref="ICommand"/> whose delegates can be attached for <see cref="InvokeExecute"/> and <see cref="InvokeCanExecute"/>.
    /// </summary>
    public abstract class DelegateCommandBase : ICommand, IActiveAware
    {
        private bool _isActive;

        private SynchronizationContext _synchronizationContext;

        readonly HashSet<string> _propertiesToObserve = new HashSet<string>();
        private INotifyPropertyChanged _inpc;

        protected DelegateCommandBase()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public virtual event EventHandler CanExecuteChanged;

        /// <summary>
        /// Raises <see cref="ICommand.CanExecuteChanged"/> so every 
        /// command invoker can requery <see cref="ICommand.CanExecute"/>.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
                    _synchronizationContext.Post((o) => handler.Invoke(this, EventArgs.Empty), null);
                else
                    handler.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises <see cref="DelegateCommandBase.CanExecuteChanged"/> so every command invoker
        /// can requery to check if the command can execute.
        /// <remarks>Note that this will trigger the execution of <see cref="DelegateCommandBase.InvokeCanExecute"/> once for each invoker.</remarks>
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        void ICommand.Execute(object parameter)
        {
            InvokeExecute(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return InvokeCanExecute(parameter);
        }

        protected abstract void InvokeExecute(object parameter);

        protected abstract bool InvokeCanExecute(object parameter);

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
        /// <returns>The current instance of DelegateCommand</returns>
        protected internal void ObservesPropertyInternal<T>(Expression<Func<T>> propertyExpression)
        {
            AddPropertyToObserve(PropertySupport.ExtractPropertyName(propertyExpression));
            HookInpc(propertyExpression.Body as MemberExpression);
        }

        protected void HookInpc(MemberExpression expression)
        {
            if (expression == null)
                return;

            if (_inpc == null)
            {
                var constantExpression = expression.Expression as ConstantExpression;
                if (constantExpression != null)
                {
                    _inpc = constantExpression.Value as INotifyPropertyChanged;
                    if (_inpc != null)
                        _inpc.PropertyChanged += Inpc_PropertyChanged;
                }
            }
        }

        protected void AddPropertyToObserve(string property)
        {
            if (_propertiesToObserve.Contains(property))
                throw new ArgumentException(String.Format("{0} is already being observed.", property));

            _propertiesToObserve.Add(property);
        }

        void Inpc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_propertiesToObserve.Contains(e.PropertyName))
                RaiseCanExecuteChanged();
        }

        #region IsActive
        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        /// <summary>
        /// Fired if the <see cref="IsActive"/> property changes.
        /// </summary>
        public virtual event EventHandler IsActiveChanged;

        /// <summary>
        /// This raises the <see cref="DelegateCommandBase.IsActiveChanged"/> event.
        /// </summary>
        protected virtual void OnIsActiveChanged()
        {
            EventHandler isActiveChangedHandler = IsActiveChanged;
            if (isActiveChangedHandler != null) isActiveChangedHandler(this, EventArgs.Empty);
        }
        #endregion
    }
}