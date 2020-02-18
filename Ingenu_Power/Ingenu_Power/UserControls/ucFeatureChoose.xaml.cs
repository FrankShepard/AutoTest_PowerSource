using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Ingenu_Power.UserControls
{
    /// <summary>
    /// ucFeatureChoose.xaml 的交互逻辑
    /// </summary>
    public partial class UcFeatureChoose : UserControl
    {
        public UcFeatureChoose()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 按键按下决定后续执行的动作是数据查询还是电源的测试过程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == BtnMeasureFeature) {
				StaticInfor.nextWindow = StaticInfor.NextWindow.NextWindow_Measure;
            } else if(button == BtnQueryFeature){
				StaticInfor.nextWindow = StaticInfor.NextWindow.NextWindow_QueryData;
            }
			this.Visibility = Visibility.Hidden;
		}
	}
}
