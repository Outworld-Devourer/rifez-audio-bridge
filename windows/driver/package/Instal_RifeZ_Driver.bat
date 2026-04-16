pnputil /add-driver "Z:\ComponentizedAudioSample.inf" /install
C:\Tools\devcon.exe install "Z:\ComponentizedAudioSample.inf" Root\sysvad_ComponentizedAudioSample
pnputil /scan-devices


pnputil /add-driver "K:\Programs\Windows_SYSVAD_FULL\Windows-driver-samples\audio\sysvad\x64\Debug\package\ComponentizedAudioSample.inf" /install
C:\Tools\devcon.exe install "K:\Programs\Windows_SYSVAD_FULL\Windows-driver-samples\audio\sysvad\x64\Debug\package\ComponentizedAudioSample.inf" Root\sysvad_ComponentizedAudioSample
