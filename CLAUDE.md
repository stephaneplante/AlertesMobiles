# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Description du projet

Ce projet contient deux parties :
- Un API web REST destiné à rouler sur un serveur Linux, qui émet les alarmes en temps réel aux clients connectés via Server-Sent Events (SSE). Principe : « connecté = destinataire » — pas d'inscription, pas de persistance, pas de reprise des alarmes manquées.
- Un client Android, développé avec .NET MAUI en C#, qui ouvre un flux SSE pour recevoir les alarmes et qui transmet une alarme à partir d'un client vers tous les autres clients connectés au clic d'un bouton.

Le périmètre est limité au réseau local et au temps réel pur ; le backlog détaillé du serveur est dans `docs/stories-serveur.md`.

## État actuel du dépôt

Le dépôt est pour l'instant vide (aucun code, aucun commit). Il faut d'abord échafauder les deux parties avant de pouvoir construire, tester ou exécuter quoi que ce soit. Dès que les projets existeront :

- `dotnet build` / `dotnet test` (et `dotnet test --filter "FullyQualifiedName~NomDuTest"` pour un test unique) dans `./serveur`
- `dotnet build --framework net10.0-android` (ou `dotnet workload install maui` d'abord si nécessaire) et `dotnet test` dans `./clients/android`

Les deux parties sont en C#/.NET : le client Android est une application .NET MAUI, pas une application native (pas de Gradle).

## Architecture (exigences non négociables)

- Le projet doit être en .NET 10 avec le langage de programmation C#.
- Le serveur d'API doit être dans le dossier `./serveur`.
- Le client doit être dans le dossier `./clients/android`.
- Le client Android doit être développé avec .NET MAUI en C#.
- Principe de Clean Architecture (les projets .NET doivent respecter la séparation Domain / Application / Infrastructure / Interface, les dépendances pointant vers le centre).
- Principes SOLID.
- Tout doit être testé unitairement, développé avec le principe TDD (tests écrits avant l'implémentation).

## Notes

- Les échanges et la documentation de ce projet sont en français.
- L'API cible une exécution sur serveur Linux.
