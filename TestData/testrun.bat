set meshdfsutil=..\src\DHI.Mesh.DfsUtil\bin\x64\Release\DHI.Mesh.DfsUtil.exe
set dfsdiff=C:\Work\DHIGitHub\MIKECore-Examples\Examples\CSharp\DHI.MikeCore.Util\bin\x64\Release\DHI.MikeCore.Util.exe -dfsdiff
%meshdfsutil% -dfsudiff odense_rough.dfsu odense_rough_quad.dfsu odense_rough_diff.dfsu
%meshdfsutil% -dfsuinterp odense_rough_quad.dfsu odense_rough.dfsu odense_rough_quad2tri.dfsu
%dfsdiff% odense_rough.dfsu odense_rough_quad2tri.dfsu odense_rough_quad2tridiff.dfsu
%dfsdiff% odense_rough_diff.dfsu odense_rough_quad2tridiff.dfsu