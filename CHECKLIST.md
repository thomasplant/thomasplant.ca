# Pixieset Clone — Build Checklist

## Phase 1: Database & Core Models
- [ ] Create `AppDbContext` in `main.Server/Data/`
- [ ] Wire up DbContext in `Program.cs` with Npgsql provider
- [ ] Define models:
  - [ ] `Gallery` (Id, Title, Slug, CoverImageUrl, CreatedAt, UpdatedAt)
  - [ ] `Photo` (Id, GalleryId, OriginalKey, ThumbnailKey, Width, Height, SortOrder, UploadedAt)
  - [ ] `Client` (Id, Name, Email, GalleryId — for client delivery)
- [ ] Run first EF migration (`dotnet ef migrations add InitialCreate`)
- [ ] Apply migration (`dotnet ef database update`)
- [ ] Verify tables in PostgreSQL via `docker compose exec db psql`

## Phase 2: MinIO (S3-Compatible Object Storage)
- [ ] Add MinIO service to `docker-compose.yml` (ports 9000 API / 9001 console)
- [ ] Install `AWSSDK.S3` NuGet package in backend
- [ ] Create `StorageService` that talks to MinIO using S3 API
- [ ] Create buckets: `originals`, `thumbnails`
- [ ] Implement upload endpoint — accept file, store original in MinIO
- [ ] Generate thumbnail on upload (ImageSharp or SkiaSharp) and store in `thumbnails` bucket
- [ ] Implement presigned URL generation for serving images
- [ ] Test uploads via MinIO Console at `localhost:9001`

## Phase 3: Backend API (Controllers)
- [ ] `GalleriesController`
  - [ ] `GET /api/galleries` — list all galleries
  - [ ] `GET /api/galleries/{slug}` — get gallery with photos
  - [ ] `POST /api/galleries` — create gallery
  - [ ] `PUT /api/galleries/{id}` — update gallery
  - [ ] `DELETE /api/galleries/{id}` — delete gallery + its photos
- [ ] `PhotosController`
  - [ ] `POST /api/galleries/{id}/photos` — upload photo(s)
  - [ ] `DELETE /api/photos/{id}` — delete photo
  - [ ] `PUT /api/photos/reorder` — update sort order
- [ ] Add request validation (FluentValidation or DataAnnotations)

## Phase 4: Frontend — Gallery Views
- [ ] Create API client service (`src/services/api.ts`)
- [ ] **Gallery List Page** (`/galleries`)
  - [ ] Fetch galleries from API
  - [ ] Display as grid of cover images with titles
- [ ] **Gallery Detail Page** (`/galleries/:slug`)
  - [ ] Masonry/grid layout like Pixieset screenshot
  - [ ] Lazy-load thumbnails with `loading="lazy"` or Intersection Observer
  - [ ] Lightbox on click (full-res image)
  - [ ] Download button (individual + bulk zip)
  - [ ] Favorites/heart button (store in localStorage or backend)
- [ ] Responsive breakpoints (1-col mobile, 2-col tablet, 3-4 col desktop)

## Phase 5: Admin Panel
- [ ] Add authentication (ASP.NET Identity or simple JWT)
- [ ] **Admin Dashboard** (`/admin`)
  - [ ] List galleries with edit/delete
  - [ ] Create new gallery form
- [ ] **Admin Gallery Editor** (`/admin/galleries/:id`)
  - [ ] Drag-and-drop photo upload (react-dropzone)
  - [ ] Reorder photos via drag-and-drop
  - [ ] Delete individual photos
  - [ ] Set cover image
- [ ] Protect admin routes (frontend guard + backend `[Authorize]`)

## Phase 6: Client Delivery Features
- [ ] Shareable gallery links (public slug-based URLs)
- [ ] Optional PIN/password protection per gallery
- [ ] Client favorites — let clients heart photos, store selections
- [ ] Download: single photo or selected photos as zip
- [ ] Optional: email notification when gallery is shared

## Phase 7: Infrastructure — Local Docker Polish
- [ ] Health checks for all services in `docker-compose.yml`
- [ ] Persistent volumes for PostgreSQL and MinIO data
- [ ] `.env.example` file with all required env vars documented
- [ ] Nginx reverse proxy in Docker (serves frontend, proxies `/api` to backend)
- [ ] HTTPS locally with self-signed certs (or mkcert)

## Phase 8: AWS Deployment
- [ ] **Networking**: Set up VPC, subnets, security groups
- [ ] **Database**: RDS PostgreSQL (or Aurora Serverless for cost)
- [ ] **Object Storage**: Swap MinIO → S3 (same SDK, change endpoint config)
  - [ ] Create S3 bucket with lifecycle rules
  - [ ] CloudFront CDN in front of S3 for image delivery
- [ ] **Compute** (pick one to learn):
  - [ ] Option A: ECS Fargate (containerized, uses your Docker image)
  - [ ] Option B: EC2 instance (more manual, learn more Linux/infra)
  - [ ] Option C: App Runner (simplest, auto-scales from container)
- [ ] **CI/CD**: GitHub Actions pipeline
  - [ ] Build Docker image
  - [ ] Push to ECR
  - [ ] Deploy to ECS/App Runner
- [ ] **DNS**: Route 53 for `thomasplant.ca`
- [ ] **HTTPS**: ACM certificate + ALB or CloudFront
- [ ] **Secrets**: AWS Secrets Manager for DB password, MinIO keys
- [ ] **Monitoring**: CloudWatch logs + basic alarms

## Phase 9: Nice-to-Haves
- [ ] Image EXIF data extraction (camera, lens, settings)
- [ ] Watermarking on download
- [ ] Custom gallery themes/colors
- [ ] Slideshow/presentation mode
- [ ] Analytics (gallery views, download counts)
- [ ] Image CDN with on-the-fly resizing (CloudFront + Lambda@Edge)

---

## Tech Stack Summary
| Layer          | Local Dev         | AWS Production      |
|----------------|-------------------|---------------------|
| Frontend       | Vite dev server   | S3 + CloudFront     |
| Backend        | .NET 10 (Docker)  | ECS Fargate / EC2   |
| Database       | PostgreSQL 18.3   | RDS PostgreSQL      |
| Object Storage | MinIO             | S3                  |
| Reverse Proxy  | Nginx (Docker)    | ALB + CloudFront    |

## Key Learning Goals (Infra)
- [ ] Container orchestration (Docker Compose → ECS)
- [ ] S3-compatible object storage (MinIO locally, S3 in prod)
- [ ] Database migrations in production
- [ ] CI/CD pipelines
- [ ] CDN and caching strategies
- [ ] VPC networking and security groups
- [ ] SSL/TLS certificate management
- [ ] Environment-based configuration
