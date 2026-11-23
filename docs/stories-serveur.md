# Stories — Serveur d'API

Serveur d'API REST (.NET 10, C#, Clean Architecture, TDD) destiné à un **réseau local uniquement**, en **temps réel pur**.

Principe directeur : **« connecté = destinataire »**. Les flux SSE ouverts sont la source de vérité ; il n'y a **pas d'inscription, pas de persistance et pas de reprise** des alarmes manquées.

Règles de périmètre :
- Livraison « au mieux » : une alarme est poussée aux flux SSE actuellement ouverts.
- Un client qui perd son flux (crash, coupure) rouvre simplement son flux ; aucune alarme en retard n'est due.
- Le `clientId` (UUID généré côté appareil) est un marqueur éphémère servant uniquement à exclure l'émetteur de sa propre alarme ; il n'est jamais stocké.
- Le serveur conserve en mémoire un dictionnaire `clientId → canal de poussée`.

## Stories

### S1 — Ouvrir un flux d'alarmes (P0)
**En tant que** client Android, **je veux** ouvrir un flux SSE **afin de** recevoir les alarmes en temps réel.

Critères d'acceptation :
- `GET /api/alarms/stream?clientId={uuid}` avec `Accept: text/event-stream` retourne `200 OK`, `Content-Type: text/event-stream`, et la connexion reste ouverte.
- L'envoi d'une alarme (S2) est reçu sous la forme d'un événement SSE dans moins de 1 s.
- Format de l'événement : `event: alarme`, `data: {"clientId": "...", "message": "..."}`.
- Le flux est fermé proprement quand le client se déconnecte (pas de fuite de connexions/canaux).
- Plusieurs flux simultanés (y compris plusieurs flux pour le même `clientId`) fonctionnent correctement.

### S2 — Émettre une alarme (P0)
**En tant que** client Android, **je veux** émettre une alarme **afin d'**alerter tous les autres clients connectés.

Critères d'acceptation :
- `POST /api/alarms` avec le corps `{ "clientId": "{uuid}", "message": "..." }` retourne `202 Accepted` et le nombre de destinataires (flux ouverts, émetteur exclu).
- L'alarme est poussée sur tous les flux SSE ouverts **sauf** celui du `clientId` émetteur.
- `POST /api/alarms` sans message (ou message vide) retourne `400 BadRequest` avec un message d'erreur descriptif.
- Si aucun autre client n'est connecté, l'appel retourne `202 Accepted` avec 0 destinataire.

### S3 — Point de santé (P1)
**En tant qu'opérateur**, **je veux** un endpoint `GET /health` **afin de** vérifier l'état du service.

Critères d'acceptation :
- `GET /health` retourne `200 OK` avec un indicateur de statut du service.

### S4 — Documentation de l'API (P2)
**En tant que** développeur du client, **je veux** une documentation des endpoints (Swagger/OpenAPI) **afin de** développer le client Android sans ambiguïté.

Critères d'acceptation :
- La définition OpenAPI expose les endpoints du domaine (flux SSE, émission d'alarme, point de santé).

## Exigences transverses

- Exécution cible : serveur Linux.
- Clean Architecture (Domain / Application / Api, dépendances pointant vers le centre) ; pas de couche Infrastructure de persistance requise (mémoire uniquement).
- Principes SOLID.
- Tout est testé unitairement, en TDD (tests écrits avant l'implémentation).

## Historique des décisions

| Décision | Statut |
|----------|--------|
| Transport des alarmes : SSE (poussé, temps réel) | ✅ Retenu |
| Liste des destinataires gérée par l'API (flux ouverts) | ✅ Retenu |
| Pas de persistance, pas de file d'attente, pas de reprise | ✅ Retenu (réseau local, temps réel pur) |
| `clientId` = UUID généré côté appareil, éphémère, sans stockage | ✅ Retenu |
| Inscription/désinscription des clients (exigence d'origine) | ❌ Abandonnée — rendue inutile par le SSE |
