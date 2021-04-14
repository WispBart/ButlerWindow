# ChangeLog

## [0.6.2-preview]
* Added ButlerWindow.OnBuildComplete(BuildReport) that gets invoked with the build report just before the build is uploaded.
* Bugfix: Default build path was not being passed to Butler correctly, resulting in an error.
* Bugfix: If ButlerWindow was open when you closed the project, it would fail to initialize correctly with the project on newer versions of Unity.

## [0.6.1-preview]
* 'Wrong Platform' error should now correctly appear when opening the window on a platform other than Windows.

## [0.6.0-preview] - 2021-03-04
* Initial Release