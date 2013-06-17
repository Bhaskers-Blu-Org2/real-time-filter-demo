﻿/**
 * Copyright (c) 2013 Nokia Corporation.
 */

namespace RealtimeFilterDemo
{
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Navigation;
    using Windows.Phone.Media.Capture;
    using Size = Windows.Foundation.Size;
    using RealtimeFilterDemo.Resources;
    using System.Windows.Media.Animation;

    /// <summary>
    /// The application main page.
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        // Constants
        private const String BackIconUri = "/Assets/Icons/back.png";
        private const String NextIconUri = "/Assets/Icons/next.png";
        private const String AboutPageUri = "/AboutPage.xaml";
        private const double MediaElementWidth = 640;
        private const double MediaElementHeight = 480;
        private const double AspectRatio = 4.0 / 3.0;

        // Members
        private PhotoCaptureDevice camera;
        private readonly ICameraEffect cameraEffect;
        private CameraStreamSource source;
        private Boolean isLoaded;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            isLoaded = false;
            cameraEffect = new NokiaImageEditingSDKEffects();
            BuildApplicationBar();
        }

        /// <summary>
        /// Creates the application bar and its items.
        /// </summary>
        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            ApplicationBarIconButton button =
                new ApplicationBarIconButton(new Uri(BackIconUri, UriKind.Relative));
            button.Text = AppResources.PreviousEffectButtonText;
            button.Click += OnBackButtonClicked;
            ApplicationBar.Buttons.Add(button);

            button = new ApplicationBarIconButton(new Uri(NextIconUri, UriKind.Relative));
            button.Text = AppResources.NextEffectButtonText;
            button.Click += OnNextButtonClicked;
            ApplicationBar.Buttons.Add(button);

            ApplicationBarMenuItem menuItem =
                new ApplicationBarMenuItem();
            menuItem.Text = AppResources.AboutPageButtonText;
            menuItem.Click += OnAboutPageButtonClicked;
            ApplicationBar.MenuItems.Add(menuItem);

            Loaded += MainPage_Loaded;
        }

        /// <summary>
        /// Opens and sets up the camera if not already. Creates a new
        /// CameraStreamSource with an effect and shows it on the screen via
        /// the media element.
        /// </summary>
        private async void Initialize()
        {
            Size mediaElementSize = new Size(MediaElementWidth, MediaElementHeight);

            if (camera == null)
            {
                // Resolve the capture resolution and open the camera
                var captureResolutions =
                    PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);

                Size selectedCaptureResolution =
                    captureResolutions.Where(
                        resolution => Math.Abs(AspectRatio - resolution.Width / resolution.Height) <= 0.1)
                            .OrderBy(resolution => resolution.Width).Last();

                camera = await PhotoCaptureDevice.OpenAsync(
                    CameraSensorLocation.Back, selectedCaptureResolution);

                // Set the image orientation prior to encoding
                camera.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation,
                    camera.SensorLocation == CameraSensorLocation.Back
                    ? camera.SensorRotationInDegrees : -camera.SensorRotationInDegrees);

                // Resolve and set the preview resolution
                var previewResolutions =
                    PhotoCaptureDevice.GetAvailablePreviewResolutions(CameraSensorLocation.Back);

                Size selectedPreviewResolution =
                    previewResolutions.Where(
                        resolution => Math.Abs(AspectRatio - resolution.Width / resolution.Height) <= 0.1)
                            .Where(resolution => (resolution.Height >= mediaElementSize.Height)
                                   && (resolution.Width >= mediaElementSize.Width))
                                .OrderBy(resolution => resolution.Width).First();

                await camera.SetPreviewResolutionAsync(selectedPreviewResolution);

                cameraEffect.CaptureDevice = camera;
            }

            if (MyCameraMediaElement.Source == null)
            {
                // Always create a new source, otherwise the MediaElement will not start
                source = new CameraStreamSource(cameraEffect, mediaElementSize);
                MyCameraMediaElement.SetSource(source);
                source.FPSChanged += OnFPSChanged;
            }
            
            // Show the index and the name of the current effect
            if (cameraEffect is NokiaImageEditingSDKEffects)
            {
                NokiaImageEditingSDKEffects effects =
                    cameraEffect as NokiaImageEditingSDKEffects;

                EffectNameTextBlock.Text =
                    (effects.EffectIndex + 1) + "/"
                    + NokiaImageEditingSDKEffects.NumberOfEffects
                    + ": " + effects.EffectName;
            }
            else
            {
                EffectNameTextBlock.Text = cameraEffect.EffectName;
            }
        }

        /// <summary>
        /// Initialises the camera and the camera stream.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage.MainPage_Loaded()");
            Initialize();
            isLoaded = true;
        }

        /// <summary>
        /// Changes the camera effect.
        /// </summary>
        /// <param name="next">If true, will increase the effect index.
        /// If false, will decrease it.</param>
        private void ChangeEffect(bool next)
        {
            if (cameraEffect == null
                || !(cameraEffect is NokiaImageEditingSDKEffects))
            {
                return;
            }

            NokiaImageEditingSDKEffects effects =
                cameraEffect as NokiaImageEditingSDKEffects;

            if (next)
            {
                if (effects.EffectIndex
                    < NokiaImageEditingSDKEffects.NumberOfEffects - 1)
                {
                    effects.EffectIndex++;
                }
                else
                {
                    effects.EffectIndex = 0;
                }
            }
            else
            {
                if (effects.EffectIndex > 0)
                {
                    effects.EffectIndex--;
                }
                else
                {
                    effects.EffectIndex =
                        NokiaImageEditingSDKEffects.NumberOfEffects - 1;
                }
            }

            EffectNameTextBlock.Text =
                (effects.EffectIndex + 1) + "/"
                + NokiaImageEditingSDKEffects.NumberOfEffects
                + ": " + cameraEffect.EffectName;
        }

        /// <summary>
        /// From PhoneApplicationPage.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedTo()");
            base.OnNavigatedTo(e);

            if (isLoaded)
            {
                Initialize();
            }
        }

        /// <summary>
        /// From PhoneApplicationPage.
        /// Sets the media element source to null and disconnects the event
        /// handling.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedFrom()");
            MyCameraMediaElement.Source = null;
            source.FPSChanged -= OnFPSChanged;
        }

        /// <summary>
        /// Removes the source from the camera media element, so that there
        /// would be no attempts to update the content of this page while
        /// about page is shown, and then opens the about page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAboutPageButtonClicked(object sender, EventArgs e)
        {
            if (camera == null)
            {
                return;
            }

            MyCameraMediaElement.Source = null;
            NavigationService.Navigate(new Uri(AboutPageUri, UriKind.Relative));
        }

        /// <summary>
        /// Changes the current camera effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            ChangeEffect(false);
        }

        /// <summary>
        /// Changes the current camera effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            ChangeEffect(true);
        }

        /// <summary>
        /// Changes the current camera effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMyCameraMediaElementTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ChangeEffect(true);
        }

        /// <summary>
        /// Updates the FPS count on the screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFPSChanged(object sender, int e)
        {
            FPSTextBlock.Text = "FPS: " + e;
        }
    }
}