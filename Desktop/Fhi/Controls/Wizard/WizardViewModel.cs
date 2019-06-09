using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Wizard
{
    public class WizardViewModel : ViewModelBase
    {
        public ObservableCollection<Step> Steps { get; }
        private Int32 _currentStep;
        private Int32 _completedStep;
        private readonly Action<Boolean> _block;
        private readonly Action _cancel;
        private readonly IProgress<WizardProgressEventArgs> _progress;

        public WizardViewModel(ObservableCollection<Step> steps, IProgress<WizardProgressEventArgs> progress , Action<Boolean> block, Action cancel)
        {
            Steps = steps;
            CurrentStep = 0;
            _completedStep = 0;
            BackCommand = new RelayCommand(Back);
            NextCommand = new RelayCommand(Next);
            SelectCommand = new RelayCommand(step => Select((Int32) step));
			FinishCommand = new RelayCommand(w => Finish(w as Window));
			CancelCommand = new RelayCommand(w => Cancel(w as Window));
			_block = block;
			_block(true);
            _progress = progress;
            _cancel = cancel;
        }

        public ICommand BackCommand { get; private set; }
        public ICommand NextCommand { get; private set; }
        public ICommand SelectCommand { get; private set; }
		public ICommand FinishCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }

		private Int32 CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (Set(ref _currentStep, value))
                {
                    RaisePropertyChanged(nameof(IsLastStep));
                    RaisePropertyChanged(nameof(IsFirstStep));
                    RaisePropertyChanged(nameof(CurrentStepModel));
                    RaisePropertyChanged(nameof(BreadCrumbs));
                    RaisePropertyChanged(nameof(Errors));
                }
            }
        }

        public Boolean IsFirstStep => CurrentStep == 0;

        public Boolean IsLastStep => CurrentStep == Steps.Count - 1;

        public WizardStepViewModel CurrentStepModel => Steps[CurrentStep].WizardModel;

        public IList<BreadCrumb> BreadCrumbs => Steps.Select((x, ix) => new BreadCrumb(x.Name, ix, this)).ToList();

        public IList<String> Errors => CurrentStepModel.GetAllErrors().Cast<String>().OrderBy(x => x.Length).ToList();

		public Boolean Finished { get; private set; }
	    
	    public Boolean ShowBreadcrumbs { get; set; }

		private void Select(Int32 step)
        {
            if (step > CurrentStep && !Validate()) return;
            if (0 <= step && step <= _completedStep)
                CurrentStep = step;
        }

        private void Back()
        {
            if (!IsFirstStep)
                CurrentStep = CurrentStep - 1;
        }

        private void Next()
        {
	        if (!Validate())
	        {
		        CurrentStepModel.ErrorsChanged += (sender, args) =>
		        {
			        RaisePropertyChanged(nameof(BreadCrumbs));
			        RaisePropertyChanged(nameof(Errors));
		        };
		        return;
	        }
			
            if (!IsLastStep)
                CurrentStep = CurrentStep + 1;
            _completedStep = Math.Max(_completedStep, CurrentStep);
        }

		private void Finish(Window window)
		{
			Finished = true;
			(Steps[CurrentStep].WizardModel as IFinishStep)?.Finish(_progress);
			window?.Close();
		}

		private Boolean Validate()
        {
            CurrentStepModel.Validate();
            RaisePropertyChanged(nameof(BreadCrumbs));
            RaisePropertyChanged(nameof(Errors));
			return !CurrentStepModel.HasErrors;
        }

		private void Cancel(Window window)
 	    {
 		    _block(false);
	         _cancel();
 			window?.Close();
 	    }

        public class Step : ViewModelBase
        {
            public String Name { get; }
            public WizardStepViewModel WizardModel { get; }

            public Step(String name, WizardStepViewModel wizardModel)
            {
                Name = name;
                WizardModel = wizardModel;
            }
        }

        public class BreadCrumb : ViewModelBase
        {
            public String Label { get; }
            public Int32 Index { get; }
            private WizardViewModel WizardModel { get; }

            public BreadCrumb(String label, Int32 index, WizardViewModel wizardModel)
            {
                Label = label;
                Index = index;
                WizardModel = wizardModel;
            }

            public Boolean IsCurrent => Index == WizardModel.CurrentStep;
            public Boolean IsCompleted => Index <= WizardModel._completedStep;
            public Boolean IsNew => Index > WizardModel._completedStep;

            public Boolean IsFirst => Index == 0;
            public Boolean IsLast => Index == WizardModel.Steps.Count - 1;

            public Boolean IsValid => IsNew || !WizardModel.Steps[Index].WizardModel.HasErrors;
        }
    }
}