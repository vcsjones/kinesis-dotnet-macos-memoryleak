# dotnet-macos-cpuusage
Min-repro example for memory leak when using AWS SDK's AmazonKinesisClient w/ dotnet 3 on macOS 10.15 Catalina.

## Program details
This program attempts to send an event to a Kinesis Data Stream every 5 seconds using the AWS SDK for .NET. After ~5 minutes of running the program and monitoring with the macOS `leaks` utility, we see an increasing memory leak involving a few different object types (detailed below).

## Context
We are running into an issue with .NET Core 3.1.2 on macOS 10.15 Catalina where running this example program for several minutes causes a memory leak. The amount of leaked memory increases steadily, and on certain machines, grows the physical memory usage of our app to **>1GB**. For reference, normal usage is **~50mb**.
Note that this memory leak occurs even with invalid AWS credentials and stream names, so a successful connection to the AWS endpoint is not required.

## How to run
### Requirements: 
- macOS 10.15 Catalina
- .NET Core 3.1.2

### Steps to reproduce:
1. Enable [MallocStackLogging][1] by setting the `MallocStackLogging` environment variable to `1`. I achieved this by editing `~/.bash_profile` to add the line `MallocStackLogging=1` and restarting the terminal.
1. Run the given program using `dotnet run Program.cs`.
2. Find the process PID using the command `ps -x | grep dotnet`. The process should look something like: 
```
28752 ttys002    0:00.67 dotnet exec ~/Downloads/kinesis-dotnet-macos-memoryleak/bin/Debug/netcoreapp3.1 kinesis_dotnet_macos_memoryleak.dll
```
3. After a few minutes, run the command `sudo leaks -hex <process-PID> | tee <path-to-output-file>`. This should pipe the result of the [leaks][2] tool to the output file. Examnining the output file should yield a result of the format:

```
Process:         dotnet [28752]
Path:            /usr/local/share/dotnet/dotnet
Load Address:    0x100e74000
Identifier:      dotnet
Version:         0
Code Type:       X86-64
Parent Process:  dotnet [28730]

Date/Time:       2020-06-02 10:47:41.966 -0700
Launch Time:     2020-06-02 10:46:11.525 -0700
OS Version:      Mac OS X 10.15.5 (19F96)
Report Version:  7
Analysis Tool:   /Applications/Xcode.app/Contents/Developer/usr/bin/leaks
Analysis Tool Version:  Xcode 11.5 (11E608c)

Physical footprint:         33.3M
Physical footprint (peak):  33.3M
----

leaks Report Version: 4.0, multi-line stacks
Process 28752: 15140 nodes malloced for 7087 KB
Process 28752: 117 leaks for 28496 total leaked bytes.

STACK OF 1 INSTANCE OF 'ROOT LEAK: <SecTrust>':
42  libsystem_pthread.dylib            0x7fff6b1cbb8b thread_start + 15
41  libsystem_pthread.dylib            0x7fff6b1d0109 _pthread_start + 148
40  libcoreclr.dylib                      0x1010c12a4 CorUnix::CPalThread::ThreadEntry(void*) + 436
39  libcoreclr.dylib                      0x10126db8f ThreadpoolMgr::WorkerThreadStart(void*) + 1311
38  libcoreclr.dylib                      0x101240154 ManagedPerAppDomainTPCount::DispatchWorkItem(bool*, bool*) + 276
37  libcoreclr.dylib                      0x101249b20 ManagedThreadBase::ThreadPool(void (*)(void*), void*) + 32
36  libcoreclr.dylib                      0x101249503 ManagedThreadBase_DispatchOuter(ManagedThreadCallState*) + 323
35  libcoreclr.dylib                      0x1012a3538 QueueUserWorkItemManagedCallback(void*) + 184
34  libcoreclr.dylib                      0x101288639 MethodDescCallSite::CallTargetWorker(unsigned long const*, unsigned long*, int) + 1657
33  libcoreclr.dylib                      0x10143c8fb CallDescrWorkerInternal + 124
32  ???                                   0x109384766 0x7fffffffffffffff + 9223372041304426343
31  ???                                   0x10924ed8d 0x7fffffffffffffff + 9223372041303158158
30  ???                                   0x109ee4798 0x7fffffffffffffff + 9223372041316353945
29  ???                                   0x109ee629a 0x7fffffffffffffff + 9223372041316360859
28  ???                                   0x10937930d 0x7fffffffffffffff + 9223372041304380174
27  ???                                   0x10938423e 0x7fffffffffffffff + 9223372041304425023
26  ???                                   0x108f57eed 0x7fffffffffffffff + 9223372041300049646
25  ???                                   0x107ef0400 0x7fffffffffffffff + 9223372041282847745
24  ???                                   0x109397586 0x7fffffffffffffff + 9223372041304503687
23  ???                                   0x10939905c 0x7fffffffffffffff + 9223372041304510557
22  ???                                   0x1093992bb 0x7fffffffffffffff + 9223372041304511164
21  ???                                   0x107ef0400 0x7fffffffffffffff + 9223372041282847745
20  ???                                   0x109397586 0x7fffffffffffffff + 9223372041304503687
19  ???                                   0x10939905c 0x7fffffffffffffff + 9223372041304510557
18  ???                                   0x1093992bb 0x7fffffffffffffff + 9223372041304511164
17  ???                                   0x107ef0400 0x7fffffffffffffff + 9223372041282847745
16  ???                                   0x109397452 0x7fffffffffffffff + 9223372041304503379
15  ???                                   0x109397631 0x7fffffffffffffff + 9223372041304503858
14  ???                                   0x109397af5 0x7fffffffffffffff + 9223372041304505078
13  ???                                   0x10939894e 0x7fffffffffffffff + 9223372041304508751
12  ???                                   0x109398ac2 0x7fffffffffffffff + 9223372041304509123
11  ???                                   0x10934c526 0x7fffffffffffffff + 9223372041304196391
10  System.Security.Cryptography.Native.Apple.dylib        0x101e816ee 0x101e7d000 + 18158
9   com.apple.security                 0x7fff3da8c909 SSLHandshake + 185
8   com.apple.security                 0x7fff3da8ca29 SSLHandshakeProceed + 185
7   libcoretls.dylib                   0x7fff68609999 tls_handshake_process + 85
6   libcoretls.dylib                   0x7fff6860a0eb SSLProcessHandshakeRecordInner + 219
5   com.apple.security                 0x7fff3dcacdac tls_verify_peer_cert + 71
4   com.apple.security                 0x7fff3dcaccff sslCreateSecTrust + 47
3   libcoretls_cfhelpers.dylib         0x7fff6861c23e tls_helper_create_peer_trust + 222
2   com.apple.security                 0x7fff3da5ff43 SecTrustCreateWithCertificates + 918
1   com.apple.CoreFoundation           0x7fff310e9663 _CFRuntimeCreateInstance + 597
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    102 (15.0K) ROOT LEAK: <SecTrust 0x7fb2136688e0> [144]
       56 (8.95K) <NSMutableArray 0x7fb213668830> [48]
          55 (8.91K) <NSMutableArray (Storage) 0x7fb213668730> [32]
             19 (3.56K) <SecCertificate 0x7fb213667360> [624]
                1 (1.50K) <CFData 0x7fb214858e00> [1536]
                1 (288 bytes) 0x7fb2136677c0 [288]
                4 (240 bytes) <NSMutableArray 0x7fb213665ac0> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213667a40> [16]
                      2 (176 bytes) <NSURL 0x7fb2136679d0> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213667990> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213667790> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213660f20> [16]
                      2 (176 bytes) <NSURL 0x7fb213667920> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb2136678e0> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213667b00> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213667b30> [16]
                      2 (176 bytes) <NSURL 0x7fb213667a90> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213667a50> [64]
                1 (224 bytes) <CFData 0x7fb213667630> [224]
                1 (128 bytes) <CFData 0x7fb213667710> [128]
                1 (96 bytes) <CFData 0x7fb2136675d0> [96]
                1 (32 bytes) 0x7fb213667b40 [32]
             17 (3.27K) <SecCertificate 0x7fb213666890> [624]
                1 (1.50K) <CFData 0x7fb214858800> [1536]
                1 (288 bytes) 0x7fb2136657b0 [288]
                4 (240 bytes) <NSMutableArray 0x7fb213665940> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213653860> [16]
                      2 (176 bytes) <NSURL 0x7fb2136658d0> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213666b00> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213665a20> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213613430> [16]
                      2 (176 bytes) <NSURL 0x7fb2136659b0> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213665970> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213665a90> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213660f10> [16]
                      2 (176 bytes) <NSURL 0x7fb2136672f0> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213665a50> [64]
                1 (144 bytes) <CFData 0x7fb213665720> [144]
                1 (32 bytes) 0x7fb213664930 [32]
             18 (2.05K) <SecCertificate 0x7fb213667b80> [624]
                1 (288 bytes) 0x7fb213667fd0 [288]
                4 (224 bytes) <NSMutableArray 0x7fb213668190> [48]
                   3 (176 bytes) <NSMutableArray (Storage) 0x7fb21365fbf0> [16]
                      2 (160 bytes) <NSURL 0x7fb213668120> [112]
                         1 (48 bytes) _clients --> <CFString 0x7fb2136680f0> [48]
                4 (224 bytes) <NSMutableArray 0x7fb213668260> [48]
                   3 (176 bytes) <NSMutableArray (Storage) 0x7fb213668290> [16]
                      2 (160 bytes) <NSURL 0x7fb2136681f0> [112]
                         1 (48 bytes) _clients --> <CFString 0x7fb2136681c0> [48]
                4 (224 bytes) <NSMutableArray 0x7fb213668340> [48]
                   3 (176 bytes) <NSMutableArray (Storage) 0x7fb213668370> [16]
                      2 (160 bytes) <NSURL 0x7fb2136682d0> [112]
                         1 (48 bytes) _clients --> <CFString 0x7fb2136682a0> [48]
                1 (224 bytes) <CFData 0x7fb213667ef0> [224]
                1 (176 bytes) <CFData 0x7fb213667e40> [176]
                1 (80 bytes) <CFData 0x7fb213667df0> [80]
                1 (32 bytes) 0x7fb213668380 [32]
       20 (4.06K) <NSMutableArray 0x7fb2137387b0> [48]
          19 (4.02K) <NSMutableArray (Storage) 0x7fb213739540> [32]
             18 (3.98K) <SecCertificate 0x7fb215038c00> [2560]
                1 (400 bytes) 0x7fb213738930 [400]
                4 (240 bytes) <NSMutableArray 0x7fb213738b70> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213738ba0> [16]
                      2 (176 bytes) <NSURL 0x7fb213738b00> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213738ac0> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213738ca0> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213738cd0> [16]
                      2 (176 bytes) <NSURL 0x7fb213738c30> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213738bf0> [64]
                4 (240 bytes) <NSMutableArray 0x7fb213738d90> [48]
                   3 (192 bytes) <NSMutableArray (Storage) 0x7fb213738dc0> [16]
                      2 (176 bytes) <NSURL 0x7fb213738d20> [112]
                         1 (64 bytes) _clients --> <CFString 0x7fb213738ce0> [64]
                1 (144 bytes) <CFData 0x7fb213738830> [144]
                1 (112 bytes) <CFData 0x7fb2137388c0> [112]
                1 (80 bytes) <CFData 0x7fb2137387e0> [80]
                1 (64 bytes) 0x7fb213738bb0 [64]
       15 (1.12K) <NSArray 0x7fb213668720> [16]
          14 (1.11K) __strong _object --> <SecPolicy 0x7fb213668800> [48]
             13 (1.06K) <NSMutableDictionary 0x7fb2136683e0> [32]
                12 (1.03K) <NSMutableDictionary (Storage) 0x7fb213668570> [368]
                   7 (480 bytes) <NSMutableArray 0x7fb213668440> [48]
                      6 (432 bytes) <NSMutableArray (Storage) 0x7fb213668510> [48]
                         1 (80 bytes) <CFData 0x7fb213668470> [80]
                         1 (80 bytes) <CFData 0x7fb2136684c0> [80]
                         1 (80 bytes) <CFData 0x7fb213668760> [80]
                         1 (80 bytes) <CFData 0x7fb2136687b0> [80]
                         1 (64 bytes) <CFData 0x7fb2136686e0> [64]  content length 0
                   2 (80 bytes) <NSMutableArray 0x7fb2136646f0> [48]
                      1 (32 bytes) <NSMutableArray (Storage) 0x7fb213668550> [32]
                   1 (64 bytes) <CFString 0x7fb2136683a0> [64]
                   1 (64 bytes) <NSDictionary 0x7fb213668400> [64]
       5 (384 bytes) 0x7fb213738080 [64]
          4 (320 bytes) 0x7fb213738190 [80]
             1 (80 bytes) 0x7fb213737ef0 [80]
             1 (80 bytes) 0x7fb2137380c0 [80]
             1 (80 bytes) 0x7fb213738140 [80]
       4 (208 bytes) 0x7fb213738280 [80]
          2 (96 bytes) 0x7fb213738310 [32]
             1 (64 bytes) 0x7fb2137382d0 [64]
          1 (32 bytes) 0x7fb213737710 [32]
       1 (128 bytes) <OS_dispatch_queue_serial 0x7fb213668860> [128]  "trust" (from Security)

STACK OF 1 INSTANCE OF 'ROOT LEAK: malloc<160>':
9   libdyld.dylib                      0x7fff6afcbcc9 start + 1
8   dotnet                                0x100e809f5 0x100e74000 + 51701
7   dotnet                                0x100e7f70b 0x100e74000 + 46859
6   dotnet                                0x100e7d746 0x100e74000 + 38726
5   dotnet                                0x100e93f55 0x100e74000 + 130901
4   dotnet                                0x100e9329a 0x100e74000 + 127642
3   libsystem_c.dylib                  0x7fff6b0440bc __opendir2$INODE64 + 68
2   libsystem_c.dylib                  0x7fff6b044193 __opendir_common + 53
1   libsystem_malloc.dylib             0x7fff6b181cf5 malloc + 21
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    2 (8.16K) ROOT LEAK: 0x7fb2136041d0 [160]
       1 (8.00K) 0x7fb21480aa00 [8192]

STACK OF 1 INSTANCE OF 'ROOT LEAK: malloc<3072>':
32  libdyld.dylib                      0x7fff6afcbcc9 start + 1
31  dotnet                                0x100e809f5 0x100e74000 + 51701
30  dotnet                                0x100e80402 0x100e74000 + 50178
29  libhostfxr.dylib                      0x100fe3bea 0x100fbf000 + 150506
28  libhostfxr.dylib                      0x100fe7be2 0x100fbf000 + 166882
27  libhostfxr.dylib                      0x100fe88ef 0x100fbf000 + 170223
26  libhostpolicy.dylib                   0x10103497e 0x101023000 + 72062
25  libhostpolicy.dylib                   0x1010339a7 0x101023000 + 68007
24  libcoreclr.dylib                      0x1010c7f72 coreclr_execute_assembly + 226
23  libcoreclr.dylib                      0x101190938 CorHost2::ExecuteAssembly(unsigned int, char16_t const*, int, char16_t const**, unsigned int*) + 504
22  libcoreclr.dylib                      0x101150388 Assembly::ExecuteMainMethod(PtrArray**, int) + 408
21  libcoreclr.dylib                      0x101150016 RunMain(MethodDesc*, short, int*, PtrArray**) + 726
20  libcoreclr.dylib                      0x101288639 MethodDescCallSite::CallTargetWorker(unsigned long const*, unsigned long*, int) + 1657
19  libcoreclr.dylib                      0x10143c8fb CallDescrWorkerInternal + 124
18  ???                                   0x107ec1b6a 0x7fffffffffffffff + 9223372041282657131
17  ???                                   0x1079dfe17 0x7fffffffffffffff + 9223372041277537816
16  ???                                   0x1079dfebe 0x7fffffffffffffff + 9223372041277537983
15  ???                                   0x1080a5334 0x7fffffffffffffff + 9223372041284637493
14  ???                                   0x1079dfe17 0x7fffffffffffffff + 9223372041277537816
13  ???                                   0x1080a555b 0x7fffffffffffffff + 9223372041284638044
12  ???                                   0x1080a557e 0x7fffffffffffffff + 9223372041284638079
11  ???                                   0x1080a55d4 0x7fffffffffffffff + 9223372041284638165
10  ???                                   0x1080a56f3 0x7fffffffffffffff + 9223372041284638452
9   ???                                   0x10792cdd2 0x7fffffffffffffff + 9223372041276804563
8   ???                                   0x107af4b3e 0x7fffffffffffffff + 9223372041278671679
7   ???                                   0x107af5ef4 0x7fffffffffffffff + 9223372041278676725
6   ???                                   0x107af55e8 0x7fffffffffffffff + 9223372041278674409
5   ???                                   0x107ad3a3d 0x7fffffffffffffff + 9223372041278536254
4   ???                                   0x107ad6e48 0x7fffffffffffffff + 9223372041278549577
3   System.Globalization.Native.dylib        0x10225c794 GlobalizationNative_CompareString + 116
2   System.Globalization.Native.dylib        0x10225bf53 CloneCollatorWithOptions + 195
1   libsystem_malloc.dylib             0x7fff6b181cf5 malloc + 21
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    1 (3.00K) ROOT LEAK: 0x7fb21500ca00 [3072]

STACK OF 10 INSTANCES OF 'ROOT LEAK: malloc<144>':
7   libsystem_pthread.dylib            0x7fff6b1cbb8b thread_start + 15
6   libsystem_pthread.dylib            0x7fff6b1d0109 _pthread_start + 148
5   libcoreclr.dylib                      0x1010c12a4 CorUnix::CPalThread::ThreadEntry(void*) + 436
4   libcoreclr.dylib                      0x10126fcc6 ThreadpoolMgr::GateThreadStart(void*) + 118
3   libcoreclr.dylib                      0x1012e53a6 EETlsSetValue(unsigned int, void*) + 22
2   libcoreclr.dylib                      0x1011938cd CExecutionEngine::CheckThreadState(unsigned int, int) + 61
1   libcoreclr.dylib                      0x10109a698 HeapAlloc + 40
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    10 (1.41K) << TOTAL >>
      1 (144 bytes) ROOT LEAK: 0x7fb213463680 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213464410 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213519580 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb21351d1f0 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213650680 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213658650 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb21365d250 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb21365fa80 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213736220 [144]
      1 (144 bytes) ROOT LEAK: 0x7fb213737060 [144]

STACK OF 1 INSTANCE OF 'ROOT LEAK: malloc<144>':
7   libsystem_pthread.dylib            0x7fff6b1cbb8b thread_start + 15
6   libsystem_pthread.dylib            0x7fff6b1d0109 _pthread_start + 148
5   libcoreclr.dylib                      0x1010c12a4 CorUnix::CPalThread::ThreadEntry(void*) + 436
4   libcoreclr.dylib                      0x10126fcc6 ThreadpoolMgr::GateThreadStart(void*) + 118
3   libcoreclr.dylib                      0x1012e53a6 EETlsSetValue(unsigned int, void*) + 22
2   libcoreclr.dylib                      0x1011938cd CExecutionEngine::CheckThreadState(unsigned int, int) + 61
1   libcoreclr.dylib                      0x10109a698 HeapAlloc + 40
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    1 (144 bytes) ROOT LEAK: 0x7fb213632930 [144]

STACK OF 1 INSTANCE OF 'ROOT LEAK: malloc<144>':
7   libsystem_pthread.dylib            0x7fff6b1cbb8b thread_start + 15
6   libsystem_pthread.dylib            0x7fff6b1d0109 _pthread_start + 148
5   libcoreclr.dylib                      0x1010c12a4 CorUnix::CPalThread::ThreadEntry(void*) + 436
4   libcoreclr.dylib                      0x10126d6ec ThreadpoolMgr::WorkerThreadStart(void*) + 124
3   libcoreclr.dylib                      0x1012e53a6 EETlsSetValue(unsigned int, void*) + 22
2   libcoreclr.dylib                      0x1011938cd CExecutionEngine::CheckThreadState(unsigned int, int) + 61
1   libcoreclr.dylib                      0x10109a698 HeapAlloc + 40
0   libsystem_malloc.dylib             0x7fff6b181d9e malloc_zone_malloc + 140 
====
    1 (144 bytes) ROOT LEAK: 0x7fb21351e110 [144]



Binary Images:
       0x100e74000 -        0x100e95ff7 +dotnet (0) <E38DAFBA-710B-3B6B-946C-025CFD1D7CA0> /usr/local/share/dotnet/dotnet
       0x100fbf000 -        0x101011ff7 +libhostfxr.dylib (0) <2AD27D3D-7785-3DB6-97F0-4EE785E0AA28> /usr/local/share/dotnet/host/fxr/3.1.4/libhostfxr.dylib
       0x101023000 -        0x101067ff7 +libhostpolicy.dylib (0) <F93D1ABE-C026-3A78-B7DE-6AC03CC6687C> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/libhostpolicy.dylib
       0x101079000 -        0x1015d6fff +libcoreclr.dylib (0) <5E542A74-7644-353B-90BC-8A1A290FAA62> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/libcoreclr.dylib
       0x101b7a000 -        0x101d9ffff +libclrjit.dylib (0) <1DE2231D-7C0F-3D0C-AE97-76E0676A709B> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/libclrjit.dylib
       0x101e7d000 -        0x101e83ff7 +System.Security.Cryptography.Native.Apple.dylib (0) <A98F02C3-3669-327A-A74A-4EB1BCA526FF> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/System.Security.Cryptography.Native.Apple.dylib
       0x102242000 -        0x10224bffb +System.Native.dylib (0) <801B62AF-1E93-3C15-8EC7-B0AF8CD49545> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/System.Native.dylib
       0x102259000 -        0x102261fff +System.Globalization.Native.dylib (0) <F9F704D5-CC31-3EE7-9845-1F9B1AEDCF9B> /usr/local/share/dotnet/shared/Microsoft.NETCore.App/3.1.4/System.Globalization.Native.dylib
       0x103ebb000 -        0x103f4ceff  dyld (750.5) <26346F4C-B18E-31A1-9964-30736214F1BF> /usr/lib/dyld
    0x7fff2cd71000 -     0x7fff2cd71fff  com.apple.Accelerate (1.11 - Accelerate 1.11) <56DFF715-6A4E-3231-BDCC-A348BCB05047> /System/Library/Frameworks/Accelerate.framework/Versions/A/Accelerate
    0x7fff2cd89000 -     0x7fff2d3dffff  com.apple.vImage (8.1 - 524.2.1) <17C93AB9-1625-3FDB-9851-C5E77BBE3428> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vImage.framework/Versions/A/vImage
    0x7fff2d3e0000 -     0x7fff2d647ff7  libBLAS.dylib (1303.60.1) <CBC28BE4-3C78-3AED-9565-0D625251D121> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libBLAS.dylib
    0x7fff2d648000 -     0x7fff2db1bfef  libBNNS.dylib (144.100.2) <8D653678-1F9B-3670-AAE2-46DFB8D37643> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libBNNS.dylib
    0x7fff2db1c000 -     0x7fff2deb7fff  libLAPACK.dylib (1303.60.1) <F8E9D081-7C60-32EC-A47D-2D30CAD73C5F> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libLAPACK.dylib
    0x7fff2deb8000 -     0x7fff2decdfec  libLinearAlgebra.dylib (1303.60.1) <D2C1ACEA-2B6A-339A-9EEB-62A76CC92CBE> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libLinearAlgebra.dylib
    0x7fff2dece000 -     0x7fff2ded3ff3  libQuadrature.dylib (7) <3112C977-8306-3190-8313-01A952B7F3CF> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libQuadrature.dylib
    0x7fff2ded4000 -     0x7fff2df44fff  libSparse.dylib (103) <40510BF9-99A7-3155-A81D-6DE5A0C73EDC> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libSparse.dylib
    0x7fff2df45000 -     0x7fff2df57fef  libSparseBLAS.dylib (1303.60.1) <3C1066AB-20D5-38D2-B1F2-70A03DE76D0B> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libSparseBLAS.dylib
    0x7fff2df58000 -     0x7fff2e12ffd7  libvDSP.dylib (735.121.1) <74702E2E-ED05-3765-B18C-64BEFF62B517> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libvDSP.dylib
    0x7fff2e130000 -     0x7fff2e1f2fef  libvMisc.dylib (735.121.1) <137558BF-503D-3A6E-96DC-A181E3FB31FF> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/libvMisc.dylib
    0x7fff2e1f3000 -     0x7fff2e1f3fff  com.apple.Accelerate.vecLib (3.11 - vecLib 3.11) <D7E8E400-35C8-3174-9956-8D1B483620DA> /System/Library/Frameworks/Accelerate.framework/Versions/A/Frameworks/vecLib.framework/Versions/A/vecLib
    0x7fff2f958000 -     0x7fff2fce6ffd  com.apple.CFNetwork (1126 - 1126) <BB8F4C63-10B8-3ACD-84CF-D4DCFA9245DD> /System/Library/Frameworks/CFNetwork.framework/Versions/A/CFNetwork
    0x7fff310e6000 -     0x7fff31565ffb  com.apple.CoreFoundation (6.9 - 1676.105) <6AF8B3CC-BC3F-3869-B9FB-1D881422364E> /System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation
    0x7fff324cd000 -     0x7fff324cdfff  com.apple.CoreServices (1069.24 - 1069.24) <D9F6AB40-10EC-3682-A969-85560E2E4768> /System/Library/Frameworks/CoreServices.framework/Versions/A/CoreServices
    0x7fff324ce000 -     0x7fff32553fff  com.apple.AE (838.1 - 838.1) <5F26DA9B-FB2E-3AF8-964B-63BD6671CF12> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/AE.framework/Versions/A/AE
    0x7fff32554000 -     0x7fff32835ff7  com.apple.CoreServices.CarbonCore (1217 - 1217) <8022AF47-AA99-3786-B086-141D84F00387> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/CarbonCore.framework/Versions/A/CarbonCore
    0x7fff32836000 -     0x7fff32883ffd  com.apple.DictionaryServices (1.2 - 323.6) <C0F3830C-A4C6-3046-9A6A-DE1B5D448C2C> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/DictionaryServices.framework/Versions/A/DictionaryServices
    0x7fff32884000 -     0x7fff3288cff7  com.apple.CoreServices.FSEvents (1268.100.1 - 1268.100.1) <E4B2CAF2-1203-335F-9971-1278CB6E2AE0> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/FSEvents.framework/Versions/A/FSEvents
    0x7fff3288d000 -     0x7fff32ac7ff6  com.apple.LaunchServices (1069.24 - 1069.24) <2E0AD228-B1CC-3645-91EE-EB7F46F2147B> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/LaunchServices
    0x7fff32ac8000 -     0x7fff32b60ff1  com.apple.Metadata (10.7.0 - 2076.6) <C8034E84-7DD4-34B9-9CDF-16A05032FF39> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/Metadata.framework/Versions/A/Metadata
    0x7fff32b61000 -     0x7fff32b8efff  com.apple.CoreServices.OSServices (1069.24 - 1069.24) <72FDEA52-7607-3745-AC43-630D80962099> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/OSServices.framework/Versions/A/OSServices
    0x7fff32b8f000 -     0x7fff32bf6fff  com.apple.SearchKit (1.4.1 - 1.4.1) <086EB5DF-A2EC-3342-8028-CA7996BE5CB2> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/SearchKit.framework/Versions/A/SearchKit
    0x7fff32bf7000 -     0x7fff32c1bff5  com.apple.coreservices.SharedFileList (131.4 - 131.4) <AE333DA2-C279-3751-8C15-B963E58EE61E> /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/SharedFileList.framework/Versions/A/SharedFileList
    0x7fff33461000 -     0x7fff33467fff  com.apple.DiskArbitration (2.7 - 2.7) <52E7D181-2A18-37CD-B24F-AA32E93F7A69> /System/Library/Frameworks/DiskArbitration.framework/Versions/A/DiskArbitration
    0x7fff337a0000 -     0x7fff33b65fff  com.apple.Foundation (6.9 - 1676.105) <1FA28BAB-7296-3A09-8E1E-E62A7D233DB8> /System/Library/Frameworks/Foundation.framework/Versions/C/Foundation
    0x7fff33ed9000 -     0x7fff33f7dff3  com.apple.framework.IOKit (2.0.2 - 1726.121.1) <A0F54725-036F-3279-A46E-C2ABDBFD479B> /System/Library/Frameworks/IOKit.framework/Versions/A/IOKit
    0x7fff37a7e000 -     0x7fff37a8affe  com.apple.NetFS (6.0 - 4.0) <AC74E6A4-6E9B-3AB1-9577-8277F8A3EDE0> /System/Library/Frameworks/NetFS.framework/Versions/A/NetFS
    0x7fff3a66c000 -     0x7fff3a688fff  com.apple.CFOpenDirectory (10.15 - 220.40.1) <BFC32EBE-D95C-3267-B95C-5CEEFD189EA6> /System/Library/Frameworks/OpenDirectory.framework/Versions/A/Frameworks/CFOpenDirectory.framework/Versions/A/CFOpenDirectory
    0x7fff3a689000 -     0x7fff3a694ffd  com.apple.OpenDirectory (10.15 - 220.40.1) <76A20BBA-775F-3E17-AB0F-FEDFCDCE0716> /System/Library/Frameworks/OpenDirectory.framework/Versions/A/OpenDirectory
    0x7fff3da2e000 -     0x7fff3dd77ff1  com.apple.security (7.0 - 59306.120.7) <AEA33464-1507-36F1-8CAE-A86EB787F9B5> /System/Library/Frameworks/Security.framework/Versions/A/Security
    0x7fff3dd78000 -     0x7fff3de00ffb  com.apple.securityfoundation (6.0 - 55236.60.1) <79289FE1-CB5F-3BEF-A33F-11A29A93A681> /System/Library/Frameworks/SecurityFoundation.framework/Versions/A/SecurityFoundation
    0x7fff3de2f000 -     0x7fff3de33ff8  com.apple.xpc.ServiceManagement (1.0 - 1) <4194D29D-F0D4-33F8-839A-D03C6C62D8DB> /System/Library/Frameworks/ServiceManagement.framework/Versions/A/ServiceManagement
    0x7fff3eadf000 -     0x7fff3eb4dff7  com.apple.SystemConfiguration (1.19 - 1.19) <0CF8726A-BE41-3E07-B895-FBC44B75450E> /System/Library/Frameworks/SystemConfiguration.framework/Versions/A/SystemConfiguration
    0x7fff42aae000 -     0x7fff42b73ff7  com.apple.APFS (1412.120.2 - 1412.120.2) <1E8FD511-FDC4-31A2-ACDE-EB5192032BC6> /System/Library/PrivateFrameworks/APFS.framework/Versions/A/APFS
    0x7fff44878000 -     0x7fff44887fd7  com.apple.AppleFSCompression (119.100.1 - 1.0) <2E75CF51-B693-3275-9A4F-40571D48745E> /System/Library/PrivateFrameworks/AppleFSCompression.framework/Versions/A/AppleFSCompression
    0x7fff46047000 -     0x7fff46050ff7  com.apple.coreservices.BackgroundTaskManagement (1.0 - 104) <F070F440-27AB-3FCF-9602-F278C332CA01> /System/Library/PrivateFrameworks/BackgroundTaskManagement.framework/Versions/A/BackgroundTaskManagement
    0x7fff48e4d000 -     0x7fff48e5dff3  com.apple.CoreEmoji (1.0 - 107.1) <CDCCB4B0-B98F-38E8-9568-C81320E756EB> /System/Library/PrivateFrameworks/CoreEmoji.framework/Versions/A/CoreEmoji
    0x7fff4949d000 -     0x7fff49507ff0  com.apple.CoreNLP (1.0 - 213) <40FC46D2-844C-3282-A8E4-69DD827F05C5> /System/Library/PrivateFrameworks/CoreNLP.framework/Versions/A/CoreNLP
    0x7fff4a382000 -     0x7fff4a3b0ffd  com.apple.CSStore (1069.24 - 1069.24) <C96E5CE8-D604-3F13-B079-B2BA33B90081> /System/Library/PrivateFrameworks/CoreServicesStore.framework/Versions/A/CoreServicesStore
    0x7fff565fa000 -     0x7fff566c8ffd  com.apple.LanguageModeling (1.0 - 215.1) <A6FAA215-9A01-3EE1-B304-2238801C5883> /System/Library/PrivateFrameworks/LanguageModeling.framework/Versions/A/LanguageModeling
    0x7fff566c9000 -     0x7fff56711fff  com.apple.Lexicon-framework (1.0 - 72) <6AE1872C-0352-36FE-90CC-7303F13A5BEF> /System/Library/PrivateFrameworks/Lexicon.framework/Versions/A/Lexicon
    0x7fff56718000 -     0x7fff5671dff3  com.apple.LinguisticData (1.0 - 353.18) <686E7B7C-640F-3D7B-A9C1-31E2DFACD457> /System/Library/PrivateFrameworks/LinguisticData.framework/Versions/A/LinguisticData
    0x7fff56f73000 -     0x7fff56f7efff  com.apple.MallocStackLogging (1.0 - 1) <0EDCEAAA-4411-3BCB-B2AF-C8D2CA689C44> /System/Library/PrivateFrameworks/MallocStackLogging.framework/Versions/A/MallocStackLogging
    0x7fff57a84000 -     0x7fff57ad0fff  com.apple.spotlight.metadata.utilities (1.0 - 2076.6) <C3AEA22D-1FEB-3E38-9821-1FA447C8AF9D> /System/Library/PrivateFrameworks/MetadataUtilities.framework/Versions/A/MetadataUtilities
    0x7fff58587000 -     0x7fff58591fff  com.apple.NetAuth (6.2 - 6.2) <D660F2CB-5A49-3DD0-9DB3-86EF0797828C> /System/Library/PrivateFrameworks/NetAuth.framework/Versions/A/NetAuth
    0x7fff6180c000 -     0x7fff6181cff3  com.apple.TCC (1.0 - 1) <FD146B21-6DC0-3B66-BB95-57A5016B1365> /System/Library/PrivateFrameworks/TCC.framework/Versions/A/TCC
    0x7fff64eee000 -     0x7fff64ef0ff3  com.apple.loginsupport (1.0 - 1) <31F02734-1ECF-37D9-9DF6-7C3BC3A324FE> /System/Library/PrivateFrameworks/login.framework/Versions/A/Frameworks/loginsupport.framework/Versions/A/loginsupport
    0x7fff67a0e000 -     0x7fff67a42fff  libCRFSuite.dylib (48) <02C52318-C537-3FD8-BBC4-E5BD25430652> /usr/lib/libCRFSuite.dylib
    0x7fff67a45000 -     0x7fff67a4ffff  libChineseTokenizer.dylib (34) <04A7CB5A-FD68-398A-A206-33A510C115E7> /usr/lib/libChineseTokenizer.dylib
    0x7fff67adb000 -     0x7fff67addff7  libDiagnosticMessagesClient.dylib (112) <27220E98-6CE2-33E3-BD48-3CC3CE4AA036> /usr/lib/libDiagnosticMessagesClient.dylib
    0x7fff67fb1000 -     0x7fff67fb2fff  libSystem.B.dylib (1281.100.1) <DC04B185-E3C9-33AF-B450-EF3ED07FB021> /usr/lib/libSystem.B.dylib
    0x7fff6803f000 -     0x7fff68040fff  libThaiTokenizer.dylib (3) <97DC10ED-3C11-3C89-B366-299A644035E7> /usr/lib/libThaiTokenizer.dylib
    0x7fff68058000 -     0x7fff6806efff  libapple_nghttp2.dylib (1.39.2) <B99D7150-D4E2-31A2-A594-36DA4B90D558> /usr/lib/libapple_nghttp2.dylib
    0x7fff680a3000 -     0x7fff68115ff7  libarchive.2.dylib (72.100.1) <20B70252-0C4B-3AFD-8C8D-F51921E9D324> /usr/lib/libarchive.2.dylib
    0x7fff681b3000 -     0x7fff681b3ff3  libauto.dylib (187) <85383E24-1592-36BC-BB39-308B7F1C826E> /usr/lib/libauto.dylib
    0x7fff68279000 -     0x7fff68289ffb  libbsm.0.dylib (60.100.1) <B2331E11-3CBB-3BCF-93A6-12627AE444D0> /usr/lib/libbsm.0.dylib
    0x7fff6828a000 -     0x7fff68296fff  libbz2.1.0.dylib (44) <BF40E193-8856-39B7-98F8-7A17B328B1E9> /usr/lib/libbz2.1.0.dylib
    0x7fff68297000 -     0x7fff682e9fff  libc++.1.dylib (902.1) <AD0805FE-F98B-3E2F-B072-83782B22DAC9> /usr/lib/libc++.1.dylib
    0x7fff682ea000 -     0x7fff682ffffb  libc++abi.dylib (902) <771E9263-E832-3985-9477-8F1B2D73B771> /usr/lib/libc++abi.dylib
    0x7fff68300000 -     0x7fff68300fff  libcharset.1.dylib (59) <FF23D4ED-A5AD-3592-9574-48486C7DF85B> /usr/lib/libcharset.1.dylib
    0x7fff68301000 -     0x7fff68312fff  libcmph.dylib (8) <296A51E6-9661-3AC2-A1C9-F1E3510F91AA> /usr/lib/libcmph.dylib
    0x7fff68313000 -     0x7fff6832afd7  libcompression.dylib (87) <21F37C2E-B9AA-38CE-9023-B763C8828AC6> /usr/lib/libcompression.dylib
    0x7fff68604000 -     0x7fff6861aff7  libcoretls.dylib (167) <9E5D1E0C-03F8-37B6-82A1-0D0597021CB8> /usr/lib/libcoretls.dylib
    0x7fff6861b000 -     0x7fff6861cfff  libcoretls_cfhelpers.dylib (167) <C23BE09B-85D1-3744-9E7B-E2B11ACD5442> /usr/lib/libcoretls_cfhelpers.dylib
    0x7fff68d42000 -     0x7fff68d42fff  libenergytrace.dylib (21) <DBF8BDEE-7229-3F06-AC10-A28DCC4243C0> /usr/lib/libenergytrace.dylib
    0x7fff68d69000 -     0x7fff68d6bfff  libfakelink.dylib (149.1) <122F530F-F10E-3DD5-BBEA-91796BE583F3> /usr/lib/libfakelink.dylib
    0x7fff68d7a000 -     0x7fff68d7ffff  libgermantok.dylib (24) <DD279BF6-E906-30D3-A69E-DC797E95F147> /usr/lib/libgermantok.dylib
    0x7fff68d8a000 -     0x7fff68e7afff  libiconv.2.dylib (59) <F58FED71-6CCA-30E8-9A51-13E9B46E568D> /usr/lib/libiconv.2.dylib
    0x7fff68e7b000 -     0x7fff690d2fff  libicucore.A.dylib (64260.0.1) <7B9204AC-EA14-3FF3-B6B9-4C85B37EED79> /usr/lib/libicucore.A.dylib
    0x7fff690ec000 -     0x7fff690edfff  liblangid.dylib (133) <36581D30-1C7B-3A58-AA07-36237BD75E0E> /usr/lib/liblangid.dylib
    0x7fff690ee000 -     0x7fff69106ff3  liblzma.5.dylib (16) <4DB30730-DBD1-3503-957A-D604049B98F9> /usr/lib/liblzma.5.dylib
    0x7fff6911e000 -     0x7fff691c5ff7  libmecab.dylib (883.11) <66AD729B-2BCC-3347-B9B3-FD88570E884D> /usr/lib/libmecab.dylib
    0x7fff691c6000 -     0x7fff69428ff1  libmecabra.dylib (883.11) <2AE744D2-AC95-3720-8E66-4F9C7A79384C> /usr/lib/libmecabra.dylib
    0x7fff698f4000 -     0x7fff69d70ff5  libnetwork.dylib (1880.120.4) <715FB943-BA01-351C-BEA6-121970472985> /usr/lib/libnetwork.dylib
    0x7fff69e11000 -     0x7fff69e44fde  libobjc.A.dylib (787.1) <CA836D3E-4595-33F1-B70C-7E39A3FBBE16> /usr/lib/libobjc.A.dylib
    0x7fff69e57000 -     0x7fff69e5bfff  libpam.2.dylib (25.100.1) <732E8D8E-C630-3EC2-B6C3-A1564E3B68B8> /usr/lib/libpam.2.dylib
    0x7fff69e5e000 -     0x7fff69e94ff7  libpcap.A.dylib (89.120.1) <CF2ADF15-2D44-3A35-94B4-DD24052F9B23> /usr/lib/libpcap.A.dylib
    0x7fff69f8c000 -     0x7fff6a176ff7  libsqlite3.dylib (308.5) <AF518115-4AD1-39F2-9B82-E2640E2221E1> /usr/lib/libsqlite3.dylib
    0x7fff6a36c000 -     0x7fff6a3c6ff8  libusrtcp.dylib (1880.120.4) <E5B0C1C5-ADF2-37CF-8F2B-B196D2266474> /usr/lib/libusrtcp.dylib
    0x7fff6a3c7000 -     0x7fff6a3caffb  libutil.dylib (57) <D33B63D2-ADC2-38BD-B8F2-24056C41E07B> /usr/lib/libutil.dylib
    0x7fff6a3cb000 -     0x7fff6a3d8ff7  libxar.1.dylib (425.2) <943A4CBB-331B-3A04-A11F-A2301189D40B> /usr/lib/libxar.1.dylib
    0x7fff6a3de000 -     0x7fff6a4c0ff7  libxml2.2.dylib (33.3) <262EF7C6-7D83-3C01-863F-36E97F5ACD34> /usr/lib/libxml2.2.dylib
    0x7fff6a4c4000 -     0x7fff6a4ecfff  libxslt.1.dylib (16.9) <86FE4382-BD77-3C19-A678-11EBCD70685A> /usr/lib/libxslt.1.dylib
    0x7fff6a4ed000 -     0x7fff6a4ffff3  libz.1.dylib (76) <DB120508-3BED-37A8-B439-5235EAB4618A> /usr/lib/libz.1.dylib
    0x7fff6adad000 -     0x7fff6adb2ff3  libcache.dylib (83) <A5ECC751-A681-30D8-B33C-D192C15D25C8> /usr/lib/system/libcache.dylib
    0x7fff6adb3000 -     0x7fff6adbefff  libcommonCrypto.dylib (60165.120.1) <C321A74A-AA91-3785-BEBF-BEDC6975026C> /usr/lib/system/libcommonCrypto.dylib
    0x7fff6adbf000 -     0x7fff6adc6fff  libcompiler_rt.dylib (101.2) <652A6012-7E5C-3F4F-9438-86BC094526F3> /usr/lib/system/libcompiler_rt.dylib
    0x7fff6adc7000 -     0x7fff6add0ff7  libcopyfile.dylib (166.40.1) <40113A69-A81C-3397-ADC6-1D16B9A22C3E> /usr/lib/system/libcopyfile.dylib
    0x7fff6add1000 -     0x7fff6ae63fe3  libcorecrypto.dylib (866.120.3) <5E4B0E50-24DD-3E04-9374-EDA9FFD6257B> /usr/lib/system/libcorecrypto.dylib
    0x7fff6af70000 -     0x7fff6afb0ff0  libdispatch.dylib (1173.100.2) <201EDBF3-0B36-31BA-A7CB-443CE35C05D4> /usr/lib/system/libdispatch.dylib
    0x7fff6afb1000 -     0x7fff6afe7fff  libdyld.dylib (750.5) <7E711A46-5E4D-393C-AEA6-440E2A5CCD0C> /usr/lib/system/libdyld.dylib
    0x7fff6afe8000 -     0x7fff6afe8ffb  libkeymgr.dylib (30) <52662CAA-DB1F-30A3-BE13-D6274B1A6D7B> /usr/lib/system/libkeymgr.dylib
    0x7fff6afe9000 -     0x7fff6aff5ff3  libkxld.dylib (6153.121.1) <F4434EE5-E521-3481-83FC-62D57DEB6B3D> /usr/lib/system/libkxld.dylib
    0x7fff6aff6000 -     0x7fff6aff6ff7  liblaunch.dylib (1738.120.8) <07CF647B-F9DC-3907-AD98-2F85FCB34A72> /usr/lib/system/liblaunch.dylib
    0x7fff6aff7000 -     0x7fff6affcff7  libmacho.dylib (959.0.1) <D91DFF00-E22F-3796-8A1C-4C1F5F8FA03C> /usr/lib/system/libmacho.dylib
    0x7fff6affd000 -     0x7fff6afffff3  libquarantine.dylib (110.40.3) <D3B7D02C-7646-3FB4-8529-B36DCC2419EA> /usr/lib/system/libquarantine.dylib
    0x7fff6b000000 -     0x7fff6b001ff7  libremovefile.dylib (48) <B5E88D9B-C2BE-3496-BBB2-C996317E18A3> /usr/lib/system/libremovefile.dylib
    0x7fff6b002000 -     0x7fff6b019ff3  libsystem_asl.dylib (377.60.2) <1170348D-2491-33F1-AA79-E2A05B4A287C> /usr/lib/system/libsystem_asl.dylib
    0x7fff6b01a000 -     0x7fff6b01aff7  libsystem_blocks.dylib (74) <7AFBCAA6-81BE-36C3-8DB0-AAE0A4ACE4C5> /usr/lib/system/libsystem_blocks.dylib
    0x7fff6b01b000 -     0x7fff6b0a2fff  libsystem_c.dylib (1353.100.2) <935DDCE9-4ED0-3F79-A05A-A123DDE399CC> /usr/lib/system/libsystem_c.dylib
    0x7fff6b0a3000 -     0x7fff6b0a6ffb  libsystem_configuration.dylib (1061.120.2) <EA9BC2B1-5001-3463-9FAF-39FF61CAC87C> /usr/lib/system/libsystem_configuration.dylib
    0x7fff6b0a7000 -     0x7fff6b0aafff  libsystem_coreservices.dylib (114) <3D0A3AA8-8415-37B2-AAE3-66C03BCE8B55> /usr/lib/system/libsystem_coreservices.dylib
    0x7fff6b0ab000 -     0x7fff6b0b3fff  libsystem_darwin.dylib (1353.100.2) <6EEC9975-EE3B-3C95-AA5B-030FD10587BC> /usr/lib/system/libsystem_darwin.dylib
    0x7fff6b0b4000 -     0x7fff6b0bbfff  libsystem_dnssd.dylib (1096.100.3) <0115092A-E61B-317D-8670-41C7C34B1A82> /usr/lib/system/libsystem_dnssd.dylib
    0x7fff6b0bc000 -     0x7fff6b0bdffb  libsystem_featureflags.dylib (17) <AFDB5095-0472-34AC-BA7E-497921BF030A> /usr/lib/system/libsystem_featureflags.dylib
    0x7fff6b0be000 -     0x7fff6b10bff7  libsystem_info.dylib (538) <851693E9-C079-3547-AD41-353F8C248BE8> /usr/lib/system/libsystem_info.dylib
    0x7fff6b10c000 -     0x7fff6b138ff7  libsystem_kernel.dylib (6153.121.1) <84D09AE3-2DA8-3F6D-ACEC-DC4990B1A2FF> /usr/lib/system/libsystem_kernel.dylib
    0x7fff6b139000 -     0x7fff6b180fff  libsystem_m.dylib (3178) <436CFF76-6A99-36F2-A3B6-8D017396A050> /usr/lib/system/libsystem_m.dylib
    0x7fff6b181000 -     0x7fff6b1a8fff  libsystem_malloc.dylib (283.100.6) <D4BA7DF2-57AC-33B0-B948-A688EE43C799> /usr/lib/system/libsystem_malloc.dylib
    0x7fff6b1a9000 -     0x7fff6b1b6ffb  libsystem_networkextension.dylib (1095.120.6) <6DE86DB0-8CD2-361E-BD6A-A34282B47847> /usr/lib/system/libsystem_networkextension.dylib
    0x7fff6b1b7000 -     0x7fff6b1c0ff7  libsystem_notify.dylib (241.100.2) <7E9E2FC8-DF26-340C-B196-B81B11850C46> /usr/lib/system/libsystem_notify.dylib
    0x7fff6b1c1000 -     0x7fff6b1c9fef  libsystem_platform.dylib (220.100.1) <736920EA-6AE0-3B1B-BBDA-7DCDF0C229DF> /usr/lib/system/libsystem_platform.dylib
    0x7fff6b1ca000 -     0x7fff6b1d4fff  libsystem_pthread.dylib (416.100.3) <77488669-19A3-3993-AD65-CA5377E2475A> /usr/lib/system/libsystem_pthread.dylib
    0x7fff6b1d5000 -     0x7fff6b1d9ff3  libsystem_sandbox.dylib (1217.120.7) <20C93D69-6452-3C82-9521-8AE54345C66F> /usr/lib/system/libsystem_sandbox.dylib
    0x7fff6b1da000 -     0x7fff6b1dcfff  libsystem_secinit.dylib (62.100.2) <E851113D-D5B1-3FB0-9D29-9C7647A71961> /usr/lib/system/libsystem_secinit.dylib
    0x7fff6b1dd000 -     0x7fff6b1e4ffb  libsystem_symptoms.dylib (1238.120.1) <25C3866B-004E-3621-9CD3-B1E9C4D887EB> /usr/lib/system/libsystem_symptoms.dylib
    0x7fff6b1e5000 -     0x7fff6b1fbff2  libsystem_trace.dylib (1147.120) <A1ED1D3A-5FAD-3559-A1D6-1BE4E1C5756A> /usr/lib/system/libsystem_trace.dylib
    0x7fff6b1fd000 -     0x7fff6b202ff7  libunwind.dylib (35.4) <253A12E2-F88F-3838-A666-C5306F833CB8> /usr/lib/system/libunwind.dylib
    0x7fff6b203000 -     0x7fff6b238ffe  libxpc.dylib (1738.120.8) <68D433B6-DCFF-385D-8620-F847FB7D4A5A> /usr/lib/system/libxpc.dylib

```

[1]: https://developer.apple.com/library/archive/documentation/Performance/Conceptual/ManagingMemory/Articles/MallocDebug.html
[2]: https://developer.apple.com/library/archive/documentation/Performance/Conceptual/ManagingMemory/Articles/FindingLeaks.html
