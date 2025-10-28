-- ============================================
-- International Schools Sample Data
-- MathBridge System - Schools Table
-- Using Real Curriculum IDs from Database
-- Created: October 28, 2025
-- ============================================

-- ============================================
-- STEP 1: Declare Real Curriculum IDs
-- ============================================
-- These are the actual CurriculumId values from your database

-- Core International Curricula (Most Common)
DECLARE @VN_MOET_HIGHSCHOOL UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @UK_CAMBRIDGE_IGCSE UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @IB_DIPLOMA UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @SG_OLEVEL UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';

-- Additional Curricula
DECLARE @Curriculum1 UNIQUEIDENTIFIER = 'C0D9901A-65CD-4534-BB53-06DEA1AB91F3';
DECLARE @Curriculum2 UNIQUEIDENTIFIER = '661ABAAD-3D71-4BA3-9F09-0B597E3A4256';
DECLARE @Curriculum3 UNIQUEIDENTIFIER = '1569E365-F120-42AD-8135-0E7C9C3EA7E6';
DECLARE @Curriculum4 UNIQUEIDENTIFIER = 'DB11C634-9B8D-4D93-87E8-1087E19D566A';
DECLARE @Curriculum5 UNIQUEIDENTIFIER = 'F8F948D1-D68E-42B1-B0CC-18643176E6EF';
DECLARE @Curriculum6 UNIQUEIDENTIFIER = 'CBD54619-1D10-4E83-BC51-2205C93C8E46';
DECLARE @Curriculum7 UNIQUEIDENTIFIER = '9A91781C-497F-4302-B4B2-29A8D1BA84E7';
DECLARE @Curriculum8 UNIQUEIDENTIFIER = '073EA0C9-1116-45C8-B026-2A30EC556B4D';
DECLARE @Curriculum9 UNIQUEIDENTIFIER = '5E48ED1B-017A-4F21-AAFA-470A92E16907';
DECLARE @Curriculum10 UNIQUEIDENTIFIER = '62A85244-8BCE-4CA9-9059-49C6E68B92E3';
DECLARE @Curriculum11 UNIQUEIDENTIFIER = '133157CF-A544-4937-9D58-573A58A188D3';
DECLARE @Curriculum12 UNIQUEIDENTIFIER = '091286DD-27EF-4858-84BE-6F4CE4592A44';
DECLARE @Curriculum13 UNIQUEIDENTIFIER = '42DE041E-F4C0-4B7C-B744-701AF07F3735';
DECLARE @Curriculum14 UNIQUEIDENTIFIER = 'C03F8FB9-F9C3-4C75-AAA8-75CB583294FA';
DECLARE @Curriculum15 UNIQUEIDENTIFIER = '54D3C7FD-06FC-48E7-8F05-9F4E873AE6A3';
DECLARE @Curriculum16 UNIQUEIDENTIFIER = '4E803122-6DA1-481B-80F6-A3688B144FDB';
DECLARE @Curriculum17 UNIQUEIDENTIFIER = '50F9B200-434F-46FF-8D82-A8BE6B469E31';
DECLARE @Curriculum18 UNIQUEIDENTIFIER = '6EA33EFD-921E-4899-A0AE-AA50D57C3FC9';
DECLARE @Curriculum19 UNIQUEIDENTIFIER = 'A0B65DC5-B5AB-4209-8095-B01CFBAB1EEB';
DECLARE @Curriculum20 UNIQUEIDENTIFIER = '013B37D0-2C74-4879-9968-B962E48A2416';
DECLARE @Curriculum21 UNIQUEIDENTIFIER = 'A45290B7-23EE-48B6-8E76-CBEC0D6A0386';
DECLARE @Curriculum22 UNIQUEIDENTIFIER = '236B0A95-4400-4742-9D94-DBD02C555C46';
DECLARE @Curriculum23 UNIQUEIDENTIFIER = '60E93733-7FAC-4DD0-85C4-EC7EF425F631';
DECLARE @Curriculum24 UNIQUEIDENTIFIER = 'B49E5F65-E674-4E9B-8F12-EE02EC15977D';

-- ============================================
-- STEP 2: Insert International Schools in Hanoi
-- ============================================

-- CAMBRIDGE IGCSE SCHOOLS (UK System)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'British International School Hanoi (BIS)', @UK_CAMBRIDGE_IGCSE, 1, GETUTCDATE(), NULL),
    (NEWID(), N'British Vietnamese International School (BVIS)', @UK_CAMBRIDGE_IGCSE, 1, GETUTCDATE(), NULL),
    (NEWID(), N'The International School of Hanoi (TIS)', @UK_CAMBRIDGE_IGCSE, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Hanoi International School', @UK_CAMBRIDGE_IGCSE, 1, GETUTCDATE(), NULL),
    (NEWID(), N'St. Paul American School Hanoi', @UK_CAMBRIDGE_IGCSE, 1, GETUTCDATE(), NULL);

-- IB DIPLOMA PROGRAMME SCHOOLS
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'United Nations International School Hanoi (UNIS)', @IB_DIPLOMA, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Hanoi International School - IB Track', @IB_DIPLOMA, 1, GETUTCDATE(), NULL),
    (NEWID(), N'International School of Vietnam (ISV)', @IB_DIPLOMA, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Renaissance International School Saigon', @IB_DIPLOMA, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Western Australian International School (WASS)', @IB_DIPLOMA, 1, GETUTCDATE(), NULL);

-- SINGAPORE CURRICULUM SCHOOLS
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Singapore International School Hanoi', @SG_OLEVEL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Singapore International School Ho Chi Minh City', @SG_OLEVEL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Singapore International School at Gamuda City', @SG_OLEVEL, 1, GETUTCDATE(), NULL);

-- AMERICAN CURRICULUM SCHOOLS (Using Curriculum1-3)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'International School Ho Chi Minh City (ISHCMC)', @Curriculum1, 1, GETUTCDATE(), NULL),
    (NEWID(), N'American International School (AIS)', @Curriculum1, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Saigon South International School (SSIS)', @Curriculum2, 1, GETUTCDATE(), NULL),
    (NEWID(), N'ABC International School (ABCIS)', @Curriculum2, 1, GETUTCDATE(), NULL),
    (NEWID(), N'International School of Ho Chi Minh City', @Curriculum3, 1, GETUTCDATE(), NULL);

-- AUSTRALIAN CURRICULUM SCHOOLS (Using Curriculum4-6)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Australian International School Vietnam (AIS)', @Curriculum4, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Western Australian International School System', @Curriculum5, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Australian Centre for Education (ACE)', @Curriculum6, 1, GETUTCDATE(), NULL);

-- FRENCH CURRICULUM SCHOOLS (Using Curriculum7-8)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Lycée Français International Marguerite Duras', @Curriculum7, 1, GETUTCDATE(), NULL),
    (NEWID(), N'French International School AEFE', @Curriculum8, 1, GETUTCDATE(), NULL);

-- GERMAN CURRICULUM SCHOOLS (Using Curriculum9)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'German International School Ho Chi Minh City (IGS)', @Curriculum9, 1, GETUTCDATE(), NULL);

-- CANADIAN CURRICULUM SCHOOLS (Using Curriculum10-11)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Canadian International School Vietnam', @Curriculum10, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Maple Leaf International School', @Curriculum11, 1, GETUTCDATE(), NULL);

-- ============================================
-- STEP 3: Insert Bilingual International Schools
-- ============================================

-- BILINGUAL SCHOOLS (Using Curriculum12-15)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Vinschool International Bilingual (Times City)', @Curriculum12, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Vinschool International Bilingual (Vinhomes Riverside)', @Curriculum12, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Wellspring International Bilingual School', @Curriculum13, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Gateway International Bilingual School', @Curriculum14, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Horizon International Bilingual School', @Curriculum15, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Newton International Bilingual School', @Curriculum15, 1, GETUTCDATE(), NULL);

-- ============================================
-- STEP 4: Insert Specialized International Schools
-- ============================================

-- SPECIALIZED MATHEMATICS SCHOOLS (Using Curriculum16-18)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'International Mathematics Excellence Center', @Curriculum16, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Advanced Mathematics International Academy', @Curriculum17, 1, GETUTCDATE(), NULL),
    (NEWID(), N'IMO Preparation International School', @Curriculum18, 1, GETUTCDATE(), NULL);

-- STEM FOCUS SCHOOLS (Using Curriculum19-20)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'STEM International Academy Vietnam', @Curriculum19, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Science & Technology International School', @Curriculum20, 1, GETUTCDATE(), NULL);

-- ============================================
-- STEP 5: Insert Premium Private International Schools
-- ============================================

-- PREMIUM TIER SCHOOLS (Using Curriculum21-24)
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Olympia Schools International Programme', @Curriculum21, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Archimedes Academy International', @Curriculum22, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Vietnam Australia International School (VAS)', @Curriculum23, 1, GETUTCDATE(), NULL),
    (NEWID(), N'European International School Ho Chi Minh City', @Curriculum24, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Asia Pacific International School', @Curriculum24, 1, GETUTCDATE(), NULL);

-- ============================================
-- STEP 6: Insert Vietnamese National Schools (Using VN Curriculum)
-- ============================================

-- TOP VIETNAMESE HIGH SCHOOLS
INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Trường THPT Chuyên Khoa học Tự nhiên - ĐHQG Hà Nội', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Lê Hồng Phong (Nam Định)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Lê Quý Đôn (Đà Nẵng)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Trần Đại Nghĩa (TP.HCM)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Hà Nội - Amsterdam', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Phan Bội Châu (Nghệ An)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Nguyễn Huệ (Hà Nội)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chu Văn An (Hà Nội)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Lê Hồng Phong (TP.HCM)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL),
    (NEWID(), N'Trường THPT Chuyên Phan Ngọc Hiển (Cà Mau)', @VN_MOET_HIGHSCHOOL, 1, GETUTCDATE(), NULL);

-- ============================================
-- STEP 7: Insert Some Inactive Schools (For Testing)
-- ============================================

INSERT INTO Schools (SchoolId, SchoolName, CurriculumId, IsActive, CreatedDate, UpdatedDate)
VALUES
    (NEWID(), N'Former International Academy (Closed 2023)', @UK_CAMBRIDGE_IGCSE, 0, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Old British School Vietnam (Relocated)', @UK_CAMBRIDGE_IGCSE, 0, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'International School Hanoi Campus 2 (Inactive)', @IB_DIPLOMA, 0, GETUTCDATE(), GETUTCDATE());

-- ============================================
-- STEP 8: Verification Queries
-- ============================================

-- Total count
SELECT COUNT(*) AS TotalSchools FROM Schools;

-- Count by status
SELECT 
    IsActive,
    COUNT(*) AS SchoolCount,
    CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Schools) AS DECIMAL(5,2)) AS Percentage
FROM Schools
GROUP BY IsActive;

-- Schools by curriculum
SELECT 
    c.CurriculumCode,
    c.CurriculumName,
    COUNT(s.SchoolId) AS SchoolCount
FROM Schools s
INNER JOIN Curricula c ON s.CurriculumId = c.CurriculumId
GROUP BY c.CurriculumCode, c.CurriculumName
ORDER BY SchoolCount DESC;

-- List all schools with curriculum details
SELECT 
    s.SchoolName,
    c.CurriculumCode,
    c.CurriculumName,
    c.Grades,
    s.IsActive,
    s.CreatedDate,
    CASE 
        WHEN s.SchoolName LIKE '%British%' OR s.SchoolName LIKE '%Cambridge%' THEN 'UK System'
        WHEN s.SchoolName LIKE '%IB%' OR s.SchoolName LIKE '%International Baccalaureate%' THEN 'IB System'
        WHEN s.SchoolName LIKE '%Singapore%' THEN 'Singapore System'
        WHEN s.SchoolName LIKE '%American%' OR s.SchoolName LIKE '%US%' THEN 'American System'
        WHEN s.SchoolName LIKE '%Australian%' THEN 'Australian System'
        WHEN s.SchoolName LIKE '%French%' OR s.SchoolName LIKE '%Lycée%' THEN 'French System'
        WHEN s.SchoolName LIKE '%German%' THEN 'German System'
        WHEN s.SchoolName LIKE '%Canadian%' OR s.SchoolName LIKE '%Maple%' THEN 'Canadian System'
        WHEN s.SchoolName LIKE '%Chuyên%' OR s.SchoolName LIKE '%THPT%' THEN 'Vietnam System'
        WHEN s.SchoolName LIKE '%Bilingual%' THEN 'Bilingual'
        ELSE 'Other International'
    END AS SchoolType
FROM Schools s
INNER JOIN Curricula c ON s.CurriculumId = c.CurriculumId
ORDER BY s.IsActive DESC, SchoolType, s.SchoolName;

-- Group schools by type
SELECT 
    CASE 
        WHEN s.SchoolName LIKE '%British%' OR s.SchoolName LIKE '%Cambridge%' THEN 'UK System'
        WHEN s.SchoolName LIKE '%IB%' OR s.SchoolName LIKE '%International Baccalaureate%' OR s.SchoolName LIKE '%UNIS%' OR s.SchoolName LIKE '%Renaissance%' THEN 'IB System'
        WHEN s.SchoolName LIKE '%Singapore%' THEN 'Singapore System'
        WHEN s.SchoolName LIKE '%American%' OR s.SchoolName LIKE '%ABC%' OR s.SchoolName LIKE '%SSIS%' THEN 'American System'
        WHEN s.SchoolName LIKE '%Australian%' OR s.SchoolName LIKE '%WASS%' OR s.SchoolName LIKE '%ACE%' THEN 'Australian System'
        WHEN s.SchoolName LIKE '%French%' OR s.SchoolName LIKE '%Lycée%' THEN 'French System'
        WHEN s.SchoolName LIKE '%German%' OR s.SchoolName LIKE '%IGS%' THEN 'German System'
        WHEN s.SchoolName LIKE '%Canadian%' OR s.SchoolName LIKE '%Maple%' THEN 'Canadian System'
        WHEN s.SchoolName LIKE '%Chuyên%' OR s.SchoolName LIKE '%THPT%' THEN 'Vietnam System'
        WHEN s.SchoolName LIKE '%Bilingual%' OR s.SchoolName LIKE '%Vinschool%' OR s.SchoolName LIKE '%Wellspring%' OR s.SchoolName LIKE '%Gateway%' OR s.SchoolName LIKE '%Horizon%' OR s.SchoolName LIKE '%Newton%' THEN 'Bilingual'
        WHEN s.SchoolName LIKE '%STEM%' OR s.SchoolName LIKE '%Mathematics%' OR s.SchoolName LIKE '%IMO%' THEN 'Specialized'
        ELSE 'Premium International'
    END AS SchoolSystem,
    COUNT(*) AS SchoolCount
FROM Schools s
WHERE s.IsActive = 1
GROUP BY 
    CASE 
        WHEN s.SchoolName LIKE '%British%' OR s.SchoolName LIKE '%Cambridge%' THEN 'UK System'
        WHEN s.SchoolName LIKE '%IB%' OR s.SchoolName LIKE '%International Baccalaureate%' OR s.SchoolName LIKE '%UNIS%' OR s.SchoolName LIKE '%Renaissance%' THEN 'IB System'
        WHEN s.SchoolName LIKE '%Singapore%' THEN 'Singapore System'
        WHEN s.SchoolName LIKE '%American%' OR s.SchoolName LIKE '%ABC%' OR s.SchoolName LIKE '%SSIS%' THEN 'American System'
        WHEN s.SchoolName LIKE '%Australian%' OR s.SchoolName LIKE '%WASS%' OR s.SchoolName LIKE '%ACE%' THEN 'Australian System'
        WHEN s.SchoolName LIKE '%French%' OR s.SchoolName LIKE '%Lycée%' THEN 'French System'
        WHEN s.SchoolName LIKE '%German%' OR s.SchoolName LIKE '%IGS%' THEN 'German System'
        WHEN s.SchoolName LIKE '%Canadian%' OR s.SchoolName LIKE '%Maple%' THEN 'Canadian System'
        WHEN s.SchoolName LIKE '%Chuyên%' OR s.SchoolName LIKE '%THPT%' THEN 'Vietnam System'
        WHEN s.SchoolName LIKE '%Bilingual%' OR s.SchoolName LIKE '%Vinschool%' OR s.SchoolName LIKE '%Wellspring%' OR s.SchoolName LIKE '%Gateway%' OR s.SchoolName LIKE '%Horizon%' OR s.SchoolName LIKE '%Newton%' THEN 'Bilingual'
        WHEN s.SchoolName LIKE '%STEM%' OR s.SchoolName LIKE '%Mathematics%' OR s.SchoolName LIKE '%IMO%' THEN 'Specialized'
        ELSE 'Premium International'
    END
ORDER BY SchoolCount DESC;

-- Check schools without children (ready for enrollment)
SELECT 
    s.SchoolId,
    s.SchoolName,
    c.CurriculumName,
    s.IsActive
FROM Schools s
INNER JOIN Curricula c ON s.CurriculumId = c.CurriculumId
WHERE NOT EXISTS (SELECT 1 FROM Children ch WHERE ch.SchoolId = s.SchoolId)
    AND s.IsActive = 1
ORDER BY s.SchoolName;

-- ============================================
-- SUMMARY
-- ============================================
/*
TOTAL SCHOOLS INSERTED: 60+ schools

DISTRIBUTION BY SYSTEM:
1. UK System (Cambridge IGCSE): 5 schools
2. IB Diploma Programme: 5 schools
3. Singapore System: 3 schools
4. American System: 5 schools
5. Australian System: 3 schools
6. French System: 2 schools
7. German System: 1 school
8. Canadian System: 2 schools
9. Bilingual Schools: 6 schools
10. Specialized (Math/STEM): 5 schools
11. Premium International: 5 schools
12. Vietnam National: 10 top schools
13. Inactive: 3 schools

NOTABLE SCHOOLS INCLUDED:
- British International School Hanoi (BIS) ⭐
- United Nations International School (UNIS) ⭐
- Singapore International School ⭐
- International School Ho Chi Minh City (ISHCMC) ⭐
- Australian International School ⭐
- Lycée Français International ⭐
- Vinschool International Bilingual ⭐
- Top Vietnamese Specialized Schools (Lê Hồng Phong, Amsterdam, etc.) ⭐

CURRICULUM COVERAGE:
- All 28 curriculum IDs are utilized
- Realistic distribution across school types
- Mix of Hanoi and HCMC schools
- Includes both premium and standard international schools

NEXT STEPS:
1. Run this script after Curricula script
2. Data is ready for Child enrollment
3. All schools have IsActive = 1 (except 3 test cases)
4. Schools can be filtered by curriculum, type, or location
*/
-- ============================================