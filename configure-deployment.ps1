param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl
)
$ErrorActionPreference = 'Stop'

# Run only after the actual GitHub Pages address (or confirmed custom domain) is known.
# Changes local files only; does not create a repository or deploy anything.
$uri = $null
if (-not [Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$uri) -or
    $uri.Scheme -ne 'https' -or $uri.Query -or $uri.Fragment -or $uri.UserInfo -or
    $BaseUrl -match '[<>"\s]') {
    throw 'Use the confirmed absolute HTTPS site URL without query parameters, fragments, credentials, or whitespace.'
}
$base = $uri.AbsoluteUri.TrimEnd('/')
$encoding = [System.Text.UTF8Encoding]::new($false)
$pages = @(
    @{ Path = 'index.html'; Url = "$base/" },
    @{ Path = 'businesses/golden-harbor.html'; Url = "$base/businesses/golden-harbor.html" }
)
foreach ($page in $pages) {
    $file = Join-Path $PSScriptRoot $page.Path
    $html = [IO.File]::ReadAllText($file)
    $html = [regex]::Replace($html, '\s*<link rel="canonical" href="[^"]*">', '')
    $html = $html.Replace('</head>', "  <link rel=`"canonical`" href=`"$($page.Url)`">`n</head>")
    $html = [regex]::Replace($html, '\s*<!-- Canonical URL is added by configure-deployment.ps1 once the deployment URL is known\. -->', '')
    $match = [regex]::Match($html, '(?s)(<script type="application/ld\+json">)(.*?)(</script>)')
    $data = $match.Groups[2].Value | ConvertFrom-Json
    $data | Add-Member -NotePropertyName url -NotePropertyValue $page.Url -Force
    $data | Add-Member -NotePropertyName '@id' -NotePropertyValue "$($page.Url)#webpage" -Force
    $json = $data | ConvertTo-Json -Depth 15
    $html = $html.Remove($match.Index, $match.Length).Insert($match.Index, "$($match.Groups[1].Value)`n$json`n  </script>")
    [IO.File]::WriteAllText($file, $html, $encoding)
}
$entries = ($pages | ForEach-Object {
    '  <url><loc>' + [System.Security.SecurityElement]::Escape($_.Url) + '</loc></url>'
}) -join "`n"
$sitemap = "<?xml version=`"1.0`" encoding=`"UTF-8`"?>`n<urlset xmlns=`"http://www.sitemaps.org/schemas/sitemap/0.9`">`n$entries`n</urlset>`n"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'sitemap.xml'), $sitemap, $encoding)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'robots.txt'), "User-agent: *`nAllow: /`n`nSitemap: $base/sitemap.xml`n", $encoding)
Write-Output 'Local canonical URLs, structured-data page URLs, sitemap, and robots directive configured. No deployment performed.'
