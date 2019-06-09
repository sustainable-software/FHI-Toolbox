using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Governance;

namespace Fhi.Controls.Indicators.Governance
{
    public class GovernanceSummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;

        private readonly String _modelName;
        
        public GovernanceSummaryViewModel(String modelName, Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;

            _modelName = modelName;
            ShowQuestionsCommand = new RelayCommand(ShowQuestions);

            Globals.Model.PropertyChanged += (sender, args) => RaisePropertyChanged("");
        }

        public IIndicator MajorIndicator => Model.Governance.FetchIndicator<Indicator>(_modelName);
        
        public IList<IIndicator> Summary => MajorIndicator?.Children;

        public ICommand ShowQuestionsCommand { get; }

        private void ShowQuestions(object parameter)
        {
            if (!(parameter is GovernanceIndicator indicator)) return;
            _navigate(new QuestionViewModel(indicator, _navigate, _back));
        }
    }
}
