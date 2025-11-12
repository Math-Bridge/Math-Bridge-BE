# StatisticsController API Endpoints

## User Statistics

### 1. `GET /api/statistics/users/overview`
- **Description**: Retrieves overall user statistics, including total users, active users, and breakdown by role.
- **Response**: JSON object with user statistics.

### 2. `GET /api/statistics/users/registrations`
- **Query Parameters**:
  - `startDate` (required): Start date for trend analysis.
  - `endDate` (required): End date for trend analysis.
- **Description**: Retrieves user registration trends over a specified period.
- **Response**: JSON object with registration trends.

### 3. `GET /api/statistics/users/location`
- **Description**: Retrieves user distribution by location.
- **Response**: JSON object with user counts by city.

### 4. `GET /api/statistics/users/wallet`
- **Description**: Retrieves wallet balance statistics for parents.
- **Response**: JSON object with totals, averages, and distribution.

---

## Session Statistics

### 5. `GET /api/statistics/sessions/overview`
- **Description**: Retrieves overall session statistics, including counts by status and completion rate.
- **Response**: JSON object with session statistics.

### 6. `GET /api/statistics/sessions/online-vs-offline`
- **Description**: Compares online and offline sessions with percentages.
- **Response**: JSON object with comparison data.

### 7. `GET /api/statistics/sessions/trends`
- **Query Parameters**:
  - `startDate` (required): Start date for trend analysis.
  - `endDate` (required): End date for trend analysis.
- **Description**: Retrieves session trends over a specified period.
- **Response**: JSON object with session trends.

---

## Tutor Statistics

### 8. `GET /api/statistics/tutors/overview`
- **Description**: Retrieves overall tutor statistics, including total tutors, average rating, and feedback breakdown.
- **Response**: JSON object with tutor statistics.

### 9. `GET /api/statistics/tutors/top-rated`
- **Query Parameters**:
  - `limit` (optional, default: 10): Number of top tutors to return.
- **Description**: Retrieves a list of top-rated tutors with their ratings.
- **Response**: JSON array of top-rated tutors.

### 10. `GET /api/statistics/tutors/most-active`
- **Query Parameters**:
  - `limit` (optional, default: 10): Number of most active tutors to return.
- **Description**: Retrieves a list of most active tutors by session count.
- **Response**: JSON array of most active tutors.

---

## Financial Statistics

### 11. `GET /api/statistics/financial/revenue`
- **Description**: Retrieves overall revenue statistics, including total revenue, transaction counts, and success rate.
- **Response**: JSON object with revenue statistics.

### 12. `GET /api/statistics/financial/revenue-trends`
- **Query Parameters**:
  - `startDate` (required): Start date for trend analysis.
  - `endDate` (required): End date for trend analysis.
- **Description**: Retrieves revenue trends over a specified period.
- **Response**: JSON object with revenue trends.
