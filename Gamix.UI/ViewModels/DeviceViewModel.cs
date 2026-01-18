using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.Core.Audio;
using Gamix.Core.Models;
using Gamix.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamix.UI.ViewModels
{
    public partial class DeviceViewModel : ObservableObject, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly ISettingsService _settingsService;
        
        private bool _isInitialized;
        private bool _shouldSaveSettings = true;
        private System.Threading.CancellationTokenSource? _devicesChangedCts;

        public ObservableCollection<AudioDevice> OutputDevices { get; } = [];
        public ObservableCollection<AudioDevice> InputDevices { get; } = [];

        [ObservableProperty]
        private AudioDevice? _selectedOutputDevice;

        [ObservableProperty]
        private AudioDevice? _selectedInputDevice;

        [ObservableProperty]
        private float _masterVolume;

        [ObservableProperty]
        private bool _isOutputMuted;

        [ObservableProperty]
        private float _inputMasterVolume;

        [ObservableProperty]
        private bool _isInputMuted;

        public DeviceViewModel(IAudioService audioService, ISettingsService settingsService)
        {
            _audioService = audioService;
            _settingsService = settingsService;
            _audioService.DevicesChanged += OnDevicesChanged;
            _audioService.DeviceMasterVolumeChanged += OnDeviceMasterVolumeChanged;
        }

        public async Task InitializeAsync()
        {
            await LoadDevicesAsync();
            await LoadMasterVolumeAsync();
            await LoadInputMasterVolumeAsync();
            
            // Start monitoring session for the initially selected device
            if (SelectedOutputDevice != null)
            {
                _audioService.StartSessionMonitoring(SelectedOutputDevice.Id);
            }
        }

        private void OnDevicesChanged()
        {
            _devicesChangedCts?.Cancel();
            _devicesChangedCts = new System.Threading.CancellationTokenSource();
            var token = _devicesChangedCts.Token;

            Task.Delay(300, token).ContinueWith(async _ =>
            {
                if (token.IsCancellationRequested) return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDevicesAsync();
                });
            }, TaskScheduler.Default);
        }

        private async Task LoadDevicesAsync()
        {
            var outputDevices = await _audioService.GetAudioDevicesAsync(true);
            UpdateDeviceList(OutputDevices, outputDevices);

            var inputDevices = await _audioService.GetAudioDevicesAsync(false);
            UpdateDeviceList(InputDevices, inputDevices);

            var savedOutputId = await _settingsService.GetSelectedOutputDeviceIdAsync();
            var savedInputId = await _settingsService.GetSelectedInputDeviceIdAsync();

            // システムのデフォルト設定を最優先、次に保存された設定、最後にリストの先頭を使用
            var targetOutput = OutputDevices.FirstOrDefault(d => d.IsDefault)
                             ?? OutputDevices.FirstOrDefault(d => d.Id == savedOutputId)
                             ?? OutputDevices.FirstOrDefault();
            
            var targetInput = InputDevices.FirstOrDefault(d => d.IsDefault)
                            ?? InputDevices.FirstOrDefault(d => d.Id == savedInputId)
                            ?? InputDevices.FirstOrDefault();

            _shouldSaveSettings = false;
            try
            {
                if (SelectedOutputDevice?.Id != targetOutput?.Id)
                {
                    SelectedOutputDevice = targetOutput;
                }
                if (SelectedInputDevice?.Id != targetInput?.Id)
                {
                    SelectedInputDevice = targetInput;
                }
            }
            finally
            {
                _shouldSaveSettings = true;
            }

            _isInitialized = true;
        }

        partial void OnSelectedOutputDeviceChanged(AudioDevice? value)
        {
            if (value != null)
            {
                if (_isInitialized)
                {
                    DefaultAudioDeviceSwitcher.SetDefaultDevice(value.Id);
                    foreach (var d in OutputDevices) d.IsDefault = (d.Id == value.Id);
                }
                
                if (_shouldSaveSettings)
                {
                    _ = _settingsService.SetSelectedOutputDeviceIdAsync(value.Id);
                }

                _ = LoadMasterVolumeAsync();
                _audioService.StartSessionMonitoring(value.Id);
            }
        }

        partial void OnSelectedInputDeviceChanged(AudioDevice? value)
        {
            if (value != null)
            {
                if (_isInitialized)
                {
                    DefaultAudioDeviceSwitcher.SetDefaultDevice(value.Id);
                    foreach (var d in InputDevices) d.IsDefault = (d.Id == value.Id);
                }

                if (_shouldSaveSettings)
                {
                    _ = _settingsService.SetSelectedInputDeviceIdAsync(value.Id);
                }

                _ = LoadInputMasterVolumeAsync();
            }
        }

        partial void OnMasterVolumeChanged(float value)
        {
            if (SelectedOutputDevice != null)
            {
                _audioService.SetDeviceMasterVolume(SelectedOutputDevice.Id, value / 100f);
            }
        }

        partial void OnInputMasterVolumeChanged(float value)
        {
            if (SelectedInputDevice != null)
            {
                _audioService.SetDeviceMasterVolume(SelectedInputDevice.Id, value);
            }
        }

        public async Task LoadMasterVolumeAsync()
        {
            if (SelectedOutputDevice != null)
            {
                var (vol, isMuted) = await _audioService.GetDeviceMasterVolumeAsync(SelectedOutputDevice.Id);
                // Avoid triggering OnMasterVolumeChanged loops if handled carefully. 
                // Setting property triggers change handler which calls SetDeviceMasterVolume.
                // However, setting the same value doesn't trigger loop.
                // But loading usually means reading FROM system.
                // We should probably check equality or use a flag, but for now simple set (assuming it matches system) is fine.
                // Or better, suppress event during load.
                // But OnMasterVolumeChanged sets system volume. If we set VM prop based on system volume, it writes back the same value. Safe enough.
                MasterVolume = vol * 100f;
                IsOutputMuted = isMuted;
            }
        }

        private async Task LoadInputMasterVolumeAsync()
        {
            if (SelectedInputDevice != null)
            {
                var (vol, isMuted) = await _audioService.GetDeviceMasterVolumeAsync(SelectedInputDevice.Id);
                InputMasterVolume = vol;
                IsInputMuted = isMuted;
            }
        }

        [RelayCommand]
        private void ToggleOutputMute()
        {
            if (SelectedOutputDevice != null)
            {
                IsOutputMuted = !IsOutputMuted;
                _audioService.SetDeviceMasterMute(SelectedOutputDevice.Id, IsOutputMuted);
            }
        }

        [RelayCommand]
        private void ToggleInputMute()
        {
            if (SelectedInputDevice != null)
            {
                IsInputMuted = !IsInputMuted;
                _audioService.SetDeviceMasterMute(SelectedInputDevice.Id, IsInputMuted);
            }
        }

        private static void UpdateDeviceList(ObservableCollection<AudioDevice> currentList, List<AudioDevice> newList)
        {
            for (int i = currentList.Count - 1; i >= 0; i--)
            {
                var current = currentList[i];
                if (!newList.Any(d => d.Id == current.Id))
                {
                    currentList.RemoveAt(i);
                }
            }

            foreach (var newDevice in newList)
            {
                var existing = currentList.FirstOrDefault(d => d.Id == newDevice.Id);
                if (existing == null)
                {
                    currentList.Add(newDevice);
                }
                else
                {
                    if (existing.Name != newDevice.Name)
                    {
                        existing.Name = newDevice.Name;
                    }
                    existing.IsDefault = newDevice.IsDefault;
                }
            }
        }

        private void OnDeviceMasterVolumeChanged(string deviceId, float volume, bool isMuted)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (SelectedOutputDevice?.Id == deviceId)
                {
                    UpdateMasterVolume(volume * 100f, isMuted);
                }
                else if (SelectedInputDevice?.Id == deviceId)
                {
                    UpdateInputMasterVolume(volume * 100f, isMuted);
                }
            });
        }

        public void UpdateMasterVolume(float volume, bool isMuted)
        {
            SetProperty(ref _masterVolume, volume, nameof(MasterVolume));
            SetProperty(ref _isOutputMuted, isMuted, nameof(IsOutputMuted));
        }

        public void UpdateInputMasterVolume(float volume, bool isMuted)
        {
            SetProperty(ref _inputMasterVolume, volume, nameof(InputMasterVolume));
            SetProperty(ref _isInputMuted, isMuted, nameof(IsInputMuted));
        }

        public void Dispose()
        {
            _audioService.DevicesChanged -= OnDevicesChanged;
            _audioService.DeviceMasterVolumeChanged -= OnDeviceMasterVolumeChanged;
            _devicesChangedCts?.Cancel();
            _devicesChangedCts?.Dispose();
        }
    }
}
