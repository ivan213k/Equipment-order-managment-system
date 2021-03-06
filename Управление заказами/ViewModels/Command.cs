﻿using System;
using System.Windows.Input;

namespace Управление_заказами.ViewModels
{
    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private readonly Action<object> execute;
        private Func<object, bool> canExecute;

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void OneExecuteChaneged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public Command(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
