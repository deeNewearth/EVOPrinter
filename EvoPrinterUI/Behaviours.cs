using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EvoPrinterUI
{
    partial class Behaviours
    {
        #region watermark
        public static readonly DependencyProperty WaterMarkProperty = DependencyProperty.RegisterAttached(
          "WaterMark",
          typeof(String),
          typeof(Behaviours),
          new FrameworkPropertyMetadata(null,
              new PropertyChangedCallback(onWaterMarkChanged)));

        struct watermarkControl
        {
            public Control ctrl { get; set; }
            public Func<object,String> TextCheckDel { get; set; }

        }

        private static void onWaterMarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var holder = new watermarkControl
            {
                ctrl = d as Control
            };

            if (holder.ctrl is TextBox)
            {
                holder.TextCheckDel = (c)=> ((TextBox)c).Text;
                ((TextBox)holder.ctrl).TextChanged += (o1, e1) =>
                {
                    ProcesswaterMark(holder);
                };
            }
            else if (holder.ctrl is PasswordBox)
            {
                holder.TextCheckDel = (c) => ((PasswordBox)c).Password;
                ((PasswordBox)holder.ctrl).PasswordChanged += (o1, e1) =>
                {
                    ProcesswaterMark(holder);
                };
            }
            else
            {
                Debug.WriteLine("Not a valid control type");
                return;
            }

            holder.ctrl.IsKeyboardFocusedChanged += (o1, e1) =>
            {
                ProcesswaterMark(holder);
            };


            holder.ctrl.SizeChanged += (o1, e1) =>
            {
                ProcesswaterMark(holder, e1.NewSize);
            };

            ProcesswaterMark(holder);
        }

        static void ProcesswaterMark(watermarkControl Holder, Size? fitTo = null)
        {
            if (Holder.ctrl.IsKeyboardFocused || !String.IsNullOrWhiteSpace(Holder.TextCheckDel(Holder.ctrl)))
            {
                Holder.ctrl.Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                var element = new Grid
                {
                    Background = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0),
                };

                if (null != fitTo)
                {
                    element.Width = fitTo.Value.Width;

                    /*
                     * No need fit height
                    if (fitTo.Value.Width > 5)
                        element.Width = fitTo.Value.Width - 5;
                    if(fitTo.Value.Height > 5)
                    element.Height = fitTo.Value.Height -5;
                     */
                }
                else
                {
                    element.Width = Holder.ctrl.ActualWidth;
                }

                element.Children.Add(new Label
                {
                    Margin = new Thickness(0),
                    Content = GetWaterMark(Holder.ctrl),
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    //FontStyle = System.Windows.FontStyles.Italic,
                    Background = System.Windows.Media.Brushes.Transparent,
                    FontSize = 10,
                    Opacity = 0.8,
                    HorizontalAlignment = HorizontalAlignment.Left,
                });

                Holder.ctrl.Background = new VisualBrush
                {
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Center,
                    Stretch = Stretch.Fill,
                    Visual = element,
                };
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static String GetWaterMark(DependencyObject element)
        {
            return (String)element.GetValue(WaterMarkProperty);
        }

        public static void SetWaterMark(DependencyObject element, String value)
        {
            element.SetValue(WaterMarkProperty, value);
        }
        #endregion

        
        
#region bindable password
      public static readonly DependencyProperty BoundPassword =
          DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(Behaviours), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));
 
      public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
          "BindPassword", typeof (bool), typeof (Behaviours), new PropertyMetadata(false, OnBindPasswordChanged));
 
      private static readonly DependencyProperty UpdatingPassword =
          DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(Behaviours), new PropertyMetadata(false));
 
      private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
          PasswordBox box = d as PasswordBox;
 
          // only handle this event when the property is attached to a PasswordBox
          // and when the BindPassword attached property has been set to true
          if (d == null || !GetBindPassword(d))
          {
              return;
          }
 
          // avoid recursive updating by ignoring the box's changed event
          box.PasswordChanged -= HandlePasswordChanged;
 
          string newPassword = (string)e.NewValue;
 
          if (!GetUpdatingPassword(box))
          {
              box.Password = newPassword;
          }
 
          box.PasswordChanged += HandlePasswordChanged;
      }
 
      private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
      {
          // when the BindPassword attached property is set on a PasswordBox,
          // start listening to its PasswordChanged event
 
          PasswordBox box = dp as PasswordBox;
 
          if (box == null)
          {
              return;
          }
 
          bool wasBound = (bool)(e.OldValue);
          bool needToBind = (bool)(e.NewValue);
 
          if (wasBound)
          {
              box.PasswordChanged -= HandlePasswordChanged;
          }
 
          if (needToBind)
          {
              box.PasswordChanged += HandlePasswordChanged;
          }
      }
 
      private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
      {
          PasswordBox box = sender as PasswordBox;
 
          // set a flag to indicate that we're updating the password
          SetUpdatingPassword(box, true);
          // push the new password into the BoundPassword property
          SetBoundPassword(box, box.Password);
          SetUpdatingPassword(box, false);
      }
 
      public static void SetBindPassword(DependencyObject dp, bool value)
      {
          dp.SetValue(BindPassword, value);
      }
 
      public static bool GetBindPassword(DependencyObject dp)
      {
          return (bool)dp.GetValue(BindPassword);
      }
 
      public static string GetBoundPassword(DependencyObject dp)
      {
          return (string)dp.GetValue(BoundPassword);
      }
 
      public static void SetBoundPassword(DependencyObject dp, string value)
      {
          dp.SetValue(BoundPassword, value);
      }
 
      private static bool GetUpdatingPassword(DependencyObject dp)
      {
          return (bool)dp.GetValue(UpdatingPassword);
      }
 
      private static void SetUpdatingPassword(DependencyObject dp, bool value)
      {
          dp.SetValue(UpdatingPassword, value);
      }
  

        #endregion
    }
}
