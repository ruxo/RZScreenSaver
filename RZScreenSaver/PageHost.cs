using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Media;
using RZScreenSaver.SlidePages;

namespace RZScreenSaver;

public class PageHost : Window{
    protected readonly IPictureSource pictureSource;
    protected ISlidePage? slide;

    IDisposable sourceSubscription = Disposable.Empty;

    public PageHost(){
        Background = Brushes.Black;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
    }
    public PageHost(IPictureSource source, Rect displayArea) : this(){
        Left = displayArea.Left;
        Top = displayArea.Top;
        Width = displayArea.Width;
        Height = displayArea.Height;
        pictureSource = source;
    }
    public ISlidePage? SlidePage{
        get => slide;
        set{
            sourceSubscription.Dispose();
            Content = slide = value;

            if (slide is null)
                sourceSubscription = Disposable.Empty;
            else
                sourceSubscription = new CompositeDisposable(
                        pictureSource.PictureChanged.Subscribe(slide!.OnShowPicture),
                        pictureSource.PictureSetChanged.Subscribe(_ => slide!.OnPictureSetChanged())
                    );
        }
    }
    public void SendToBottom(){
        this.SetNoActivate();
        this.SetBottomMost();
    }
}