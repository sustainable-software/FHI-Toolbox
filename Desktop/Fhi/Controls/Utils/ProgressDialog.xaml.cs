using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Fhi.Controls.Utils
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }
        
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(String), typeof(ProgressDialog), new PropertyMetadata(""));

        public String Label
        {
            get { return (String)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register("IsIndeterminate", typeof(Boolean), typeof(ProgressDialog), new PropertyMetadata(true));

        public Boolean IsIndeterminate
        {
            get { return (Boolean)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }

        public static readonly DependencyProperty IsCancellableProperty =
            DependencyProperty.Register("IsCancellable", typeof(Boolean), typeof(ProgressDialog), new PropertyMetadata(false));

        public Boolean IsCancellable
        {
            get { return (Boolean)GetValue(IsCancellableProperty); }
            set { SetValue(IsCancellableProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(Double), typeof(ProgressDialog), new PropertyMetadata(0.0));

        public Double Value
        {
            get { return (Double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }


        // Prevent manual closing.
        private Boolean _canClose;

        private void ProgressDialog_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !_canClose;
        }

        private new void Close()
        {
            _canClose = true;
            base.Close();
        }


        // Cancellation.
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }


        // Executes task on background thread & shows modal progress dialog.
        // Task should not call any UI methods.
        public TR Execute<TR>(Func<CancellationToken, IProgress<Int32>, TR> func)
        {
            _completed = false;
            var task = Task
                .Run(() => func(_cancellationTokenSource.Token, new Progress<Int32>(p => Dispatcher.Invoke(() => { Value = p; }))))
                .ContinueWith(t =>
                {
                    _completed = true;
                    Dispatcher.Invoke(Close);
                    return t.Result;
                });
            if (!_completed)
                ShowDialog();
            return task.IsCanceled ? default(TR) : task.Result;
        }

        public void Execute(Action<CancellationToken, IProgress<Int32>> func)
        {
            Execute((ct, p) =>
            {
                func(ct, p);
                return true;
            });
        }

        private volatile Boolean _completed = false;
    }
}
