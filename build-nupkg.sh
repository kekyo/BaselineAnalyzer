#!/bin/bash

# BaselineAnalyzer
# Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
#
# Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo ""
echo "==========================================================="
echo "Build BaselineAnalyzer"
echo ""

dotnet build -p:Configuration=Release -p:Platform="Any CPU" -p:RestoreNoCache=True BaselineAnalyzer.sln
dotnet pack -p:Configuration=Release -p:Platform="Any CPU" -o artifacts BaselineAnalyzer.sln
