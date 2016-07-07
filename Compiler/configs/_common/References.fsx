#if HOSTED
// do not import anything when hosted. The build script import the correct libraries.
#else
#I __SOURCE_DIRECTORY__
#r @"..\..\bin\Debug\BespokeJS.Library.dll"
#endif