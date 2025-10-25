$ErrorActionPreference = "Stop"

$src = "Docs"
$dst = "com.pippapips.zenecs.core/Documentation~"
New-Item -ItemType Directory -Force -Path $dst | Out-Null

# from→to map
$files = @(
  @{ from = "overview/faq.md";                     to = "faq.md" }
  @{ from = "getting-started/quickstart-basic.md"; to = "quickstart.md" }
)

@"
# ZenECS Core — Documentation (Package)

Start here:
- Quickstart: `quickstart.md`
- FAQ: `faq.md`

**Full docs:** https://your-docs-site.example

> This package ships a minimal doc set. The website is the source of truth.
"@ | Set-Content -Path (Join-Path $dst "index.md") -Encoding UTF8

foreach ($f in $files) {
  $from = Join-Path $src $f.from
  $to   = Join-Path $dst $f.to
  New-Item -ItemType Directory -Force -Path (Split-Path $to) | Out-Null
  Copy-Item $from $to -Force
}

Write-Host ("Synced {0} files to {1}" -f $files.Count, $dst)
