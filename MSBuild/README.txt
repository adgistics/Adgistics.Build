###############################################################################
# Copyright (c) Adgistics Limited and others.
# All rights reserved. The contents of this file are subject to
# the terms of the Adgistics Development and Distribution License
# (the "License"). You may not use this file except in compliance
# with the License.
#
# http://www.adgistics.com/license.html
#
# See the License for the specific language governing permissions
# and limitations under the License.
###############################################################################

This Directory contains modified version of:

    * MSBuild Extension Pack v3.5.11.0
    * MSBuild Community Tasks v1.4.0.42

The libraries are installed in MSBuild/lib. The modified target files are in
MSBuild, the modification is in the PropertyGroup that locates the
"MSBuildExtensionsPath" as we are always going to load our custom libraries
from our MSBuild directory we need to change this to "MSBuildProjectDirectory",
for example:

    <ExtensionTasksPath>
      $(MSBuildExtensionsPath)\ExtensionPack\
    </ExtensionTasksPath>

    <ExtensionTasksPath>
      $(MSBuildProjectDirectory)\..\MSBuild\lib\ExtensionPack\
    </ExtensionTasksPath>

The back reference ("..") is needed as the main build targets are always outside
of the current project.

