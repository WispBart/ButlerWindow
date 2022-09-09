# ChangeLog
## [1.0.1]
* Added an option to request confirmation before uploading, so you can check your build locally first.
* 'Windows' buildtarget now builds to StandaloneWindows64 instead of StandaloneWindows.

## [1.0.0]
* Added support for building from MacOS.
* Added support for building to Android (APK file).
* If the build failed for any reason, ButlerWindow no longer tries to upload it.

## [0.6.2-preview]
* Added ButlerWindow.OnBuildComplete(BuildReport) that gets invoked with the build report just before the build is uploaded.
* Bugfix: Default build path was not being passed to Butler correctly, resulting in an error.
* Bugfix: If ButlerWindow was open when you closed the project, it would fail to initialize correctly with the project on newer versions of Unity.

## [0.6.1-preview]
* 'Wrong Platform' error should now correctly appear when opening the window on a platform other than Windows.

## [0.6.0-preview] - 2021-03-04
* Initial Release
