using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ingenu_Power.Domain;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.UserControls
{
    /// <summary>
    /// ucLogin.xaml 的交互逻辑
    /// </summary>
    public partial class UcDataQuery : UserControl
    {
        public UcDataQuery( )
        {
            InitializeComponent();			
		}

		public void CalendarDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
		{
			//Calendar_Start.SelectedDate = (( PickersViewModel )DataContext).Date;
			Calendar_Start.SelectedDate = DateTime.Now;
		}

		public void CalendarDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
		{
			if (!Equals( eventArgs.Parameter, "1" )) return;

			if (!Calendar_Start.SelectedDate.HasValue) {
				eventArgs.Cancel();
				return;
			}

			(( PickersViewModel )DataContext).Date = Calendar_Start.SelectedDate.Value;
		}

	}
}
