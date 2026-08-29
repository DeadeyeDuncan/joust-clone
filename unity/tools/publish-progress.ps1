# Regenerate the progress site and publish it to GitHub Pages.
#
# Run at every gate, after the findings document has been updated and any new
# screenshots have been captured into unity/artifacts/screenshots/.
#
#   powershell -ExecutionPolicy Bypass -File unity/tools/publish-progress.ps1
#
# Pages serves from the gh-pages branch root, because GitHub only allows a
# branch root or /docs as a Pages source — never an arbitrary folder such as
# site/. The site subtree is pushed to that branch.

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')

Push-Location $repo
try {
    python unity/tools/build-progress-site.py
    if ($LASTEXITCODE -ne 0) { throw "site generation failed ($LASTEXITCODE)" }

    $dirty = git status --porcelain -- site
    if ($dirty) {
        git add site
        git commit -m "docs: refresh progress site"
        Write-Output 'site changes committed'
    }
    else {
        Write-Output 'site unchanged, nothing to commit'
    }

    git subtree push --prefix=site origin gh-pages
    Write-Output 'published: https://deadeyeduncan.github.io/joust-clone/'
}
finally {
    Pop-Location
}
