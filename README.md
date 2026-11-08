MewUIDemo
=======

## 简介  
c#跨平台UI框架MewUI使用例子，演示了内置组件和扩展组件。一套代码支持windows、linux、mac平台。  
注意Webview2例子是windows平台独占，其他平台编译注释webview代码即可。

## Windows编译  
安装dotnet 10 sdk。
Program.cs 里Main方法改为如下  

```csharp
static void Main()
{
    //windows系统运行下面两行
    Win32Platform.Register();
    MewVGWin32Backend.Register();

    var mainWindow = CreateMainWindow();
    Application.Run(mainWindow);
}  
```

## Linux编译  
安装dotnet 10 sdk。 
Program.cs 里Main方法改为如下  

```csharp
static void Main()
{
    X11Platform.Register();
    MewVGX11Backend.Register();

    var mainWindow = CreateMainWindow();
    Application.Run(mainWindow);
}
```

## Mac编译  
安装dotnet 10 sdk。 
Program.cs 里Main方法改为如下 

```csharp
static void Main() 
{
    MacOSPlatform.Register();
	MewVGMacOSBackend.Register();

    var mainWindow = CreateMainWindow();
    Application.Run(mainWindow);
}
```

## Demo
### demo_windows:  
![image](demo_image/demo_windows.png)   

### demo_linux:  
![image](demo_image/demo_linux.png)   

### demo_border:  
![image](demo_image/demo_border.png) 

### demo_button:  
![image](demo_image/demo_button.png)  

### demo_buttongroup:  
![image](demo_image/demo_buttongroup.png) 

### demo_calendar:  
![image](demo_image/demo_calendar.png) 

### demo_cartesianchart:  
![image](demo_image/demo_cartesianchart.png) 

### demo_checkbox:  
![image](demo_image/demo_checkbox.png) 

### demo_colorpicker:  
![image](demo_image/demo_colorpicker.png) 

### demo_combobox:  
![image](demo_image/demo_combobox.png)

### demo_consolebox:  
![image](demo_image/demo_consolebox.png) 

### demo_contentcontrol:  
![image](demo_image/demo_contentcontrol.png)

### demo_contextmenu:  
![image](demo_image/demo_contextmenu.png) 

### demo_datepicker:  
![image](demo_image/demo_datepicker.png) 

### demo_dispatchertimer:  
![image](demo_image/demo_dispatchertimer.png) 

### demo_dockpanel:  
![image](demo_image/demo_dockpanel.png) 

### demo_drawingboard:  
![image](demo_image/demo_drawingboard.png) 

### demo_dropdownbutton:  
![image](demo_image/demo_dropdownbutton.png)

### demo_expander:  
![image](demo_image/demo_expander.png)

### demo_exswitch:  
![image](demo_image/demo_exswitch.png)

### demo_filedialog:  
![image](demo_image/demo_filedialog.png)

### demo_flowgraph:  
![image](demo_image/demo_flowgraph.png)

### demo_gridview:  
![image](demo_image/demo_gridview.png)

### demo_groupbox:  
![image](demo_image/demo_groupbox.png)

### demo_iconbutton:  
![image](demo_image/demo_iconbutton.png)

### demo_image:  
![image](demo_image/demo_image.png)

### demo_itemcontrol:  
![image](demo_image/demo_itemcontrol.png)

### demo_label:  
![image](demo_image/demo_label.png)

### demo_listbox:  
![image](demo_image/demo_listbox.png)

### demo_listboxcustom:  
![image](demo_image/demo_listboxcustom.png)

### demo_lunardatebox:  
![image](demo_image/demo_lunardatebox.png)

### demo_markdown:  
![image](demo_image/demo_markdown.png)

### demo_markuptextblock:  
![image](demo_image/demo_markuptextblock.png)

### demo_menubar:  
![image](demo_image/demo_menubar.png)

### demo_messagebox:  
![image](demo_image/demo_messagebox.png)

### demo_multilinetextbox:  
![image](demo_image/demo_multilinetextbox.png)

### demo_navigationview:  
![image](demo_image/demo_navigationview.png)

### demo_numericupdown:  
![image](demo_image/demo_numericupdown.png)

### demo_piechart:  
![image](demo_image/demo_piechart.png)

### demo_polarchart:  
![image](demo_image/demo_polarchart.png)

### demo_popup:  
![image](demo_image/demo_popup.png)

### demo_progressbar:  
![image](demo_image/demo_progressbar.png)

### demo_progressring:  
![image](demo_image/demo_progressring.png)

### demo_propertygrid:  
![image](demo_image/demo_propertygrid.png)

### demo_radiobutton:  
![image](demo_image/demo_radiobutton.png)

### demo_repeatbutton:  
![image](demo_image/demo_repeatbutton.png)

### demo_scrollviewer:  
![image](demo_image/demo_scrollviewer.png)

### demo_segmentedcontrol:  
![image](demo_image/demo_segmentedcontrol.png)

### demo_separator:  
![image](demo_image/demo_separator.png)

### demo_slider:  
![image](demo_image/demo_slider.png)

### demo_splitbutton:  
![image](demo_image/demo_splitbutton.png)

### demo_splitpanel:  
![image](demo_image/demo_splitpanel.png)

### demo_stackpanel:  
![image](demo_image/demo_stackpanel.png)

### demo_syntaxviewer:  
![image](demo_image/demo_syntaxviewer.png)

### demo_tabcontrol:  
![image](demo_image/demo_tabcontrol.png)

### demo_textblock:  
![image](demo_image/demo_textblock.png)

### demo_togglebutton:  
![image](demo_image/demo_togglebutton.png)

### demo_toggleswitch:  
![image](demo_image/demo_toggleswitch.png)

### demo_toolbar:  
![image](demo_image/demo_toolbar.png)

### demo_tooltip:  
![image](demo_image/demo_tooltip.png)

### demo_transitioncontentcontrol:  
![image](demo_image/demo_transitioncontentcontrol.png)

### demo_treeview:  
![image](demo_image/demo_treeview.png)

### demo_uniformgrid:  
![image](demo_image/demo_uniformgrid.png)

### demo_usercontrol:  
![image](demo_image/demo_usercontrol.png)

### demo_webview2:  
![image](demo_image/demo_webview2.png)

### demo_wrappanel:  
![image](demo_image/demo_wrappanel.png)