#!/bin/bash

# Custom test runner with nice formatting
echo "🧪 SharedKernel Test Suite"
echo "=========================="
echo ""

# Run tests with detailed console output
dotnet test --logger "console;verbosity=detailed" --nologo --no-build 2>/dev/null | \
grep -E "(Passed|Failed|Skipped)" | \
sed 's/  Passed /✅ PASS: /' | \
sed 's/  Failed /❌ FAIL: /' | \
sed 's/  Skipped /⏭️  SKIP: /' | \
sort

echo ""
echo "📊 Summary:"
dotnet test --logger "console;verbosity=minimal" --nologo
