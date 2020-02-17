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
        /// 下次需要显示的界面的类型
        /// </summary>
        public enum NextWindow : int
        {
            /// <summary>
            /// 下次需要显示的界面保持不变
            /// </summary>
            NextWindow_Now = 0,
            /// <summary>
            /// 下次需要显示的界面是产品测试界面
            /// </summary>
            NextWindow_Measure,
            /// <summary>
            /// 下次需要显示的界面是数据查询界面
            /// </summary>
            NextWindow_QueryData,
        } ;

        /// <summary>
        /// 默认的点击按键之后需要显示的界面是产品测试界面
        /// </summary>
        public NextWindow nextWindow = NextWindow.NextWindow_Now;

        /// <summary>
        /// 按键按下决定后续执行的动作是数据查询还是电源的测试过程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == BtnMeasureFeature) {
                nextWindow = NextWindow.NextWindow_Measure;
            } else if(button == BtnQueryFeature){
                nextWindow = NextWindow.NextWindow_QueryData;
            }
        }
    }
}
