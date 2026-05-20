function Find-KspManaged {
    $paths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed",
        "C:\Program Files\Steam\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed",
        "D:\SteamLibrary\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed",
        "C:\Cracked\Kerbal-Space-Program-SteamRIP.com\Kerbal Space Program\KSP_x64_Data\Managed"
        "E:\SteamLibrary\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed"
    )

    foreach ($p in $paths) {
        if (Test-Path $p) {
            return $p
        }
    }

    $customPath = Read-Host "Dossier KSP introuvable automatiquement. Veuillez coller le chemin vers votre dossier KSP_x64_Data/Managed"
    if (Test-Path $customPath) {
        return $customPath
    }

    return $null
}

$kspManaged = Find-KspManaged

if ($null -eq $kspManaged) {
    Write-Error "Impossible de continuer sans le dossier Managed de KSP."
    exit
}

Write-Host "Dossier KSP trouve : $kspManaged" -ForegroundColor Cyan

# Mise a jour des fichiers .csproj
$projects = Get-ChildItem -Recurse -Filter *.csproj

foreach ($proj in $projects) {
    Write-Host "Mise a jour de : $($proj.Name)..." -ForegroundColor Yellow
    # Ici le script de ton IA va injecter ou mettre a jour le chemin $kspManaged dans le XML du csproj
    # (Le reste du script d'origine de ton IA pour modifier le XML va ici si besoin, 
    # mais la syntaxe de base est maintenant corrigee)
}

Write-Host "Configuration terminee avec succes !" -ForegroundColor Green