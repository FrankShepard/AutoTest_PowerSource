   M S B A M L   `   `   `  ����G  CIngenu_Power, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:Ingenu_Power.DomainIngenu_Power.Domain  5   	   O Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem 5          Ingenu_Power.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   U QMaterialDesignThemes.Wpf, Version=3.0.1.920, Culture=neutral, PublicKeyToken=nullFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   .domain!clr-namespace:Ingenu_Power.Domain  5   	   3system&clr-namespace:System;assembly=mscorlib  5      -   5           MaterialDesignBody#���   5
   	     MaterialDesignPaper#���  5   	   $��Medium"�5   	   $��14(�5   	     � Icon$  Resources/owl.ico��5       � Title$ 盈帜电源测试系统��5   	   $	��494��6   $	��494��6'   $	��734��68   $	��734��6D   $��Center=�6S   $��Center=�6p    � WindowStartupLocation$ CenterScreen=�6�    � ToolTip$3 -用于盈帜电源系列产品的自动测试��6�    � Topmost .  $��Center=�6�   .��5      )  #MaterialDesignThemes.Wpf.DialogHost     
Identifier$ 
RootDialog��6   .��5      )  #MaterialDesignThemes.Wpf.DrawerHost    MaterialDesignDivider#���  6   $
��Auto��6T   $
��Auto��6b      LeftDrawerContent 5      �� 5      .+�5      �� $��10q�6   $
��Left=�6   功能选项6>   �� 5      -   5        $��BtnMenu_ConnectDatabase��5   �      CloseDrawerCommand#��Z 6   $��5q�6Q   "  System.Windows.Controls.Dock   Left#��Z 6\   $
��Left=�6�     MaterialDesignFlatButton#���  6�   .��5      � .+�5   	   '  !MaterialDesignThemes.Wpf.PackIcon  	   Kind$	 DatabasePlus=�6!   $��5q�65   $��Center=�6@   �� 5   	   $
��Wrap=�6   $��重连数据库��6'   $
��auto��64   $��Center=�6A   $
��Left=�6\   5   	   5      �� 5      -   5        $��BtnMenu_Login��5   �   #��Z 6   $��5q�6Q   #��Z 6\   $
��Left=�6�   #���  6�   .��5      � .+�5   	     $	 LoginVariant=�6!   $��5q�65   $��Center=�6@   �� 5   	   $
��Wrap=�6   $��重新登陆��6'   $
��auto��63   $��Center=�6@   $
��Left=�6[   5    	   5!      �� 5"      -   5        $��BtnMenu_InstumentValidate��5"   �   #��Z 6   $��5q�6Q   #��Z 6\   $
��Left=�6�   #���  6�   .��5#      � .+�5$   	     $	 Verified=�6!   $��5q�61   $��Center=�6<   �� 5%   	   $
��Wrap=�6   $��仪表通讯校验��6'   $
��auto��65   $��Center=�6B   $
��Left=�6]   5&   	   5'      �� 5(      -   5        $��BtnMenu_ISP��5(   �   #��Z 6   $��5q�6Q   #��Z 6\   $
��Left=�6�   #���  6�   .��5)      � .+�5*   	     $	 Download=�6!   $��5q�61   $��Center=�6<   �� 5+   	   $
��Wrap=�6   $��单片机ISP��6'   $
��auto��65   $��Center=�6B   $
��Left=�6]   5,   	   5-      �� 5.      -   5        $��BtnMenu_Measure��5.   �   #��Z 6   $��5q�6Q   #��Z 6\   $
��Left=�6�   #���  6�   .��5/      � .+�50   	     $	 ScaleBalance=�6!   $��5q�65   $��Center=�6@   �� 51   	   $
��Wrap=�6   $��电源测试��6'   $
��auto��63   $��Center=�6@   $
��Left=�6[   52   	   53      �� 54      -   5        $��BtnMenu_DataQuery��54   �   #��Z 6   $��5q�6Q   #��Z 6\   $
��Left=�6�   #���  6�   .��55      � .+�56   	     $	 Magnify=�6!   $��5q�60   $��Center=�6;   �� 57   	   $
��Wrap=�6   $��数据查询��6'   $
��auto��63   $��Center=�6@   $
��Left=�6[   58   	   59      5:      .��5=      � 
 � RowDefinitions
 5>      �� 5?      $��25 �6   �� 5@      $��* �6   .+�5C      � $��0��6   .+�5D      (  "MaterialDesignThemes.Wpf.ColorZone  $��0q�6    +  %MaterialDesignThemes.Wpf.ShadowAssist   ShadowDepth$ Depth2=�6,      Mode$ 
PrimaryMid=�6]   $	��Top=�6o   .��5E      � $��24��6   .+�5F   	   � $
��Left=�6   .+�5G   
   d� -   5        $��BtnMenuShow��5G   �    $  MaterialDesignActionToggleButton#��[ 6   $��Right=�6Q   $��24��6m   $��0q�6y   $��24��6�      OpenDrawerCommand#��Z 6�   #��Z 6�   ��5H        5I      $	 Settings=�6$   $��0q�64   1  +MaterialDesignThemes.Wpf.ToggleButtonAssist   	OnContent 5K        5L      $	 Settings=�6$   $�� 0q�64   5N        5O   
   -	   5        $��
PkiSyncDll��5O   "   $	 	CloudSync=�64   $��20��6E   $��20��6Q   $��2q�6\   6�   5P   
   �� 5Q   	   $$��盈帜电源自动测试系统��6   $��Center=�6%   $��Center=�6@   ���	#FFB0A5A5$��YouYuan)�6t   $��Italic$�6�   $��16(�6�   $��Normal"�6�   � 5R   	   $��Right=�6   .+�5S   
   d� -
   5        $��
BtnMessage��5S   �   #��[ 6   $��Right=�6Q   $��24��6m   $��0q�6y   $��24��6�   $' !最近一次出现的错误信息��6�   ��5T        5U      $	 MessageTextOutline=�6$   $��0q�6>    5W        5X      $	 MessageTextOutline=�6$   $�� 0q�6>   5Z      d� 5[   
   -   5        $��BtnInfor��5[   �   #��[ 6   $��Right=�6Q   $��24��6m   $��0q�6y   $��24��6�   ��5\        5]      $	 InformationVariant=�6$   $��0q�6>    5_        5`      $	 InformationVariant=�6$   $�� 0q�6>   5b      5c   
   5d   	   5e      5f      � 5g      -   5        $��GrdMain��5g      $��1��6   ��5h      �� 5i       �� ImageSource$# Resources/BackGroundImage.png��6    �� Opacity$	 0.8Q�6?   5k      5l      5m      5n      5o      