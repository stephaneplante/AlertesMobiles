# Stories — Client Android

Client Android (.NET 10, C#, .NET MAUI, Clean Architecture, TDD) destiné à un **réseau local uniquement**, en **temps réel pur**.

Principe directeur : **« connecté = destinataire »**. Le client reçoit les alarmes tant que son flux SSE est ouvert ; il n'y a **pas d'inscription et pas de reprise** des alarmes manquées (voir `stories-serveur.md`).

Règles de périmètre :
- Le client consomme l'API décrite dans `stories-serveur.md` (`GET /api/alarms/stream`, `POST /api/alarms`, `GET /health`).
- Le `clientId` (UUID) est généré côté appareil ; il sert uniquement à ne pas recevoir ses propres alarmes.
- Une alarme manquée (flux coupé, application fermée) est perdue ; le client ne cherche pas à la récupérer.
- Aucune authentification, aucun compte utilisateur.

## Stories

### C1 — Configurer l'adresse du serveur (P0)
**En tant qu'**utilisateur, **je veux** saisir l'adresse du serveur **afin de** me connecter au serveur de mon réseau local.

Critères d'acceptation :
- Un écran de paramètres permet de saisir l'adresse (ex. `http://192.168.1.50:5000`).
- L'adresse est validée (format d'URL valide, schéma `http` ou `https`) avant d'être enregistrée ; sinon un message d'erreur descriptif est affiché.
- L'adresse est conservée entre deux lancements de l'application.
- Un bouton « Tester la connexion » appelle `GET /health` et indique clairement le succès ou l'échec.

### C2 — Identifier l'appareil (P0)
**En tant que** client, **je veux** disposer d'un `clientId` (UUID) **afin de** ne pas recevoir mes propres alarmes.

Critères d'acceptation :
- Un UUID est généré au premier lancement et réutilisé aux lancements suivants.
- Le même `clientId` est utilisé pour le flux SSE et pour l'émission d'alarmes.
- Aucune donnée personnelle n'est associée au `clientId`.

### C3 — Se connecter au flux d'alarmes (P0)
**En tant qu'**utilisateur, **je veux** que l'application ouvre un flux SSE **afin de** recevoir les alarmes en temps réel.

Critères d'acceptation :
- Au démarrage (et après enregistrement d'une adresse valide), le client ouvre `GET /api/alarms/stream?clientId={uuid}` avec `Accept: text/event-stream`.
- Les événements `event: alarme` sont décodés (`clientId`, `message`) ; les événements inconnus ou mal formés sont ignorés sans interrompre le flux.
- La connexion reste ouverte indéfiniment (pas de délai d'expiration de lecture sur le flux).
- Le flux est fermé proprement quand l'utilisateur quitte l'application ou se déconnecte explicitement.

### C4 — Être alerté à la réception d'une alarme (P0)
**En tant qu'**utilisateur, **je veux** être alerté dès qu'une alarme arrive **afin de** réagir immédiatement.

Critères d'acceptation :
- À la réception d'un événement `alarme`, l'appareil joue un son, vibre et affiche le message de l'alarme.
- L'alerte est visible au premier plan (écran dédié ou bannière) et sous forme de notification si l'application est en arrière-plan.
- Les alarmes reçues sont affichées dans un historique **de session** (en mémoire, non persisté), de la plus récente à la plus ancienne.
- Le délai entre l'émission sur le serveur et l'alerte est inférieur à 1 s sur un réseau local sain.

### C5 — Émettre une alarme (P0)
**En tant qu'**utilisateur, **je veux** déclencher une alarme d'un clic **afin d'**alerter tous les autres clients connectés.

Critères d'acceptation :
- Un bouton d'alarme bien visible envoie `POST /api/alarms` avec `{ "clientId": "...", "message": "..." }`.
- Le message est saisissable ; s'il est vide, l'envoi est bloqué côté client avec un message explicite (le serveur répondrait `400`).
- Sur `202 Accepted`, l'application affiche le nombre de destinataires retourné (y compris « 0 destinataire »).
- Sur erreur réseau, `400` ou `5xx`, un message d'erreur clair est affiché et le bouton redevient utilisable.
- Un clic répété pendant l'envoi ne déclenche pas de doublon (bouton désactivé pendant la requête).
- L'émetteur ne reçoit pas sa propre alarme (garanti par le `clientId`).

### C6 — Se reconnecter automatiquement (P0)
**En tant qu'**utilisateur, **je veux** que l'application rouvre le flux après une coupure **afin de** continuer à recevoir les alarmes sans intervention.

Critères d'acceptation :
- En cas de perte du flux (Wi-Fi coupé, serveur redémarré), le client retente la connexion automatiquement avec un délai croissant plafonné (ex. 1 s, 2 s, 5 s, puis 10 s max).
- Au retour du réseau, la reconnexion intervient rapidement.
- Aucune alarme manquée pendant la coupure n'est réclamée ni affichée après coup.
- Les tentatives s'arrêtent si l'utilisateur se déconnecte explicitement.

### C7 — Voir l'état de la connexion (P1)
**En tant qu'**utilisateur, **je veux** voir si je suis connecté **afin de** savoir si je suis en mesure de recevoir les alarmes.

Critères d'acceptation :
- Un indicateur visible affiche l'un des états : *Connecté*, *Connexion en cours*, *Reconnexion*, *Déconnecté*.
- L'état change dans les 2 s suivant une perte ou un rétablissement du flux.
- Si l'adresse du serveur n'est pas configurée, l'indicateur invite à la renseigner.

### C8 — Recevoir les alarmes en arrière-plan (P1)
**En tant qu'**utilisateur, **je veux** continuer à recevoir les alarmes quand l'application n'est pas au premier plan **afin de** ne rien manquer.

Critères d'acceptation :
- Le flux SSE est maintenu par un service de premier plan (*foreground service*) avec une notification persistante indiquant l'état de la connexion.
- L'application demande la permission de notification (Android 13+) et guide l'utilisateur si elle est refusée.
- L'application explique comment exclure l'appli de l'optimisation de batterie si le système coupe le service.
- L'utilisateur peut arrêter le service (et donc la réception) depuis la notification ou l'application.

### C9 — Couper le son d'une alarme (P2)
**En tant qu'**utilisateur, **je veux** pouvoir arrêter le son et la vibration d'une alarme **afin de** reprendre le contrôle de mon appareil.

Critères d'acceptation :
- Un bouton « Arrêter » est disponible sur l'alerte et sur la notification.
- L'action est purement locale : aucun appel au serveur, aucun effet sur les autres clients.
- Le son s'arrête de lui-même après une durée maximale raisonnable.

## Exigences transverses

- Plateforme cible : Android, application **.NET MAUI** en C# (pas d'application native, pas de Gradle).
- Clean Architecture (Domain / Application / Infrastructure / UI MVVM, dépendances pointant vers le centre) :
  - *Domain* : entités `Alarme`, `ClientId`.
  - *Application* : cas d'usage et abstractions (`IAlarmStreamClient`, `IAlarmApi`, `INotificateurAlarme`, `IParametres`).
  - *Infrastructure* : client HTTP/SSE, stockage des paramètres, notifications, service Android.
  - *UI* : pages, ViewModels, binding.
- Principes SOLID ; dépendances injectées, aucune logique métier dans le code-behind.
- Tout est testé unitairement, en TDD (tests écrits avant l'implémentation) ; le client SSE et l'API sont testés derrière leurs abstractions avec des doubles de test.
- Trafic HTTP en clair autorisé vers le réseau local (configuration de sécurité réseau Android à prévoir, car le cleartext est bloqué par défaut).
- Les textes de l'interface sont en français.

## Historique des décisions

| Décision | Statut |
|----------|--------|
| Réception des alarmes par flux SSE (client maintient la connexion) | ✅ Retenu |
| `clientId` = UUID généré à l'appareil, conservé localement, jamais associé à une identité | ✅ Retenu |
| Pas de reprise des alarmes manquées (aucun `Last-Event-ID`) | ✅ Retenu (aligné sur le serveur) |
| Historique des alarmes reçues : mémoire de session uniquement | ✅ Retenu |
| Découverte automatique du serveur (mDNS) | ⏸ Hors périmètre — adresse saisie manuellement |
| Alarmes poussées via FCM quand l'app est fermée | ❌ Écarté — réseau local, temps réel pur |
