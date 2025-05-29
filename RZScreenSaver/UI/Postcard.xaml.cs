using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace RZScreenSaver.UI;

/// <summary>
/// Interaction logic for Postcard.xaml
/// </summary>
[ContentProperty("Source")]
public partial class Postcard{
    const int NormalShadowAngle = 315;
    public Postcard() {
        InitializeComponent();
    }
    public Size Size{
        get { return new Size(picture.Width, picture.Height); }
        set{
            picture.Width = value.Width;
#if DEBUG
            if (Math.Abs(picture.Height - value.Height) > 0.001){
                Debug.WriteLine("Height mismatch: " + picture.Height + " and " + value.Height);
            }
#endif
        }
    }
    public double Angle{
        get => cardRotator.Angle;
        set{
            cardRotator.Angle = value;
            shadow.Direction = NormalShadowAngle + value;
        }
    }
    public ImageSource Source{
        get => picture.Source;
        set => picture.Source = value;
    }
}