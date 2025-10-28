-- ============================================
-- High School International Curriculum Data
-- MathBridge System - Curricula Table
-- Focus: International Programs for Grades 9-12
-- Created: October 28, 2025
-- ============================================

-- NOTE: Run this script BEFORE inserting Schools data
-- This script focuses on HIGH SCHOOL (Secondary) international curricula

-- ============================================
-- STEP 1: Clear existing data (Optional)
-- ============================================
-- Uncomment ONLY if you want to reset curriculum data:
-- DELETE FROM Schools WHERE CurriculumId IS NOT NULL;
-- DELETE FROM Curricula;

-- ============================================
-- STEP 2: Insert High School International Curricula
-- ============================================

-- CAMBRIDGE INTERNATIONAL PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Cambridge IGCSE (Grades 9-10)
    (
        '22222222-2222-2222-2222-222222222222',
        'UK-CAMBRIDGE-IGCSE',
        'Cambridge International General Certificate of Secondary Education (IGCSE)',
        '9,10',
        'https://www.cambridgeinternational.org/programmes-and-qualifications/cambridge-igcse/',
        1,
        GETUTCDATE(),
        NULL,
        140
    ),
    
    -- Cambridge International AS & A Level (Grades 11-12)
    (
        NEWID(),
        'UK-CAMBRIDGE-ALEVEL',
        'Cambridge International AS & A Level',
        '11,12',
        'https://www.cambridgeinternational.org/programmes-and-qualifications/cambridge-international-as-and-a-levels/',
        1,
        GETUTCDATE(),
        NULL,
        180
    ),
    
    -- Cambridge Pre-U
    (
        NEWID(),
        'UK-CAMBRIDGE-PREU',
        'Cambridge Pre-U (Advanced Level Alternative)',
        '11,12',
        'https://www.cambridgeinternational.org/programmes-and-qualifications/cambridge-pre-u/',
        1,
        GETUTCDATE(),
        NULL,
        175
    );

-- INTERNATIONAL BACCALAUREATE (IB) PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- IB Middle Years Programme (Grades 9-10)
    (
        NEWID(),
        'IB-MYP',
        'International Baccalaureate Middle Years Programme (MYP)',
        '9,10',
        'https://www.ibo.org/programmes/middle-years-programme/',
        1,
        GETUTCDATE(),
        NULL,
        150
    ),
    
    -- IB Diploma Programme (Grades 11-12)
    (
        '33333333-3333-3333-3333-333333333333',
        'IB-DP',
        'International Baccalaureate Diploma Programme (IBDP)',
        '11,12',
        'https://www.ibo.org/programmes/diploma-programme/',
        1,
        GETUTCDATE(),
        NULL,
        200
    ),
    
    -- IB Career-related Programme
    (
        NEWID(),
        'IB-CP',
        'IB Career-related Programme (IBCP)',
        '11,12',
        'https://www.ibo.org/programmes/career-related-programme/',
        1,
        GETUTCDATE(),
        NULL,
        180
    );

-- AMERICAN HIGH SCHOOL PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Advanced Placement (AP)
    (
        NEWID(),
        'US-AP',
        'Advanced Placement (AP) Program - College Board',
        '10,11,12',
        'https://apstudents.collegeboard.org/',
        1,
        GETUTCDATE(),
        NULL,
        170
    ),
    
    -- American High School Diploma
    (
        NEWID(),
        'US-HIGH-SCHOOL',
        'American High School Diploma Program',
        '9,10,11,12',
        'https://www.ed.gov/k-12reforms/standards',
        1,
        GETUTCDATE(),
        NULL,
        160
    ),
    
    -- SAT Subject Tests Aligned
    (
        NEWID(),
        'US-SAT-ALIGNED',
        'SAT Subject Test Aligned Curriculum',
        '10,11,12',
        'https://collegereadiness.collegeboard.org/sat-subject-tests',
        1,
        GETUTCDATE(),
        NULL,
        165
    );

-- FRENCH BACCALAURÉAT
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- French Baccalauréat (Bac)
    (
        NEWID(),
        'FR-BACCALAUREAT',
        'French Baccalauréat (Bac) - Mathematics Track',
        '10,11,12',
        'https://www.education.gouv.fr/le-baccalaureat',
        1,
        GETUTCDATE(),
        NULL,
        185
    ),
    
    -- French International Baccalauréat (OIB)
    (
        NEWID(),
        'FR-OIB',
        'Option Internationale du Baccalauréat (OIB)',
        '10,11,12',
        'https://www.education.gouv.fr/sections-internationales',
        1,
        GETUTCDATE(),
        NULL,
        190
    );

-- AUSTRALIAN PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Australian Year 10
    (
        NEWID(),
        'AU-YEAR10',
        'Australian Curriculum Year 10',
        '10',
        'https://www.australiancurriculum.edu.au/f-10-curriculum/mathematics/',
        1,
        GETUTCDATE(),
        NULL,
        140
    ),
    
    -- Australian Senior Secondary (ATAR)
    (
        NEWID(),
        'AU-ATAR',
        'Australian Tertiary Admission Rank (ATAR) Program',
        '11,12',
        'https://www.australiancurriculum.edu.au/',
        1,
        GETUTCDATE(),
        NULL,
        175
    ),
    
    -- Victorian Certificate of Education (VCE)
    (
        NEWID(),
        'AU-VCE',
        'Victorian Certificate of Education (VCE) - Mathematics',
        '11,12',
        'https://www.vcaa.vic.edu.au/curriculum/vce/',
        1,
        GETUTCDATE(),
        NULL,
        170
    );

-- SINGAPORE PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- O-Level (Secondary 3-4)
    (
        '44444444-4444-4444-4444-444444444444',
        'SG-OLEVEL',
        'Singapore-Cambridge GCE O-Level',
        '9,10',
        'https://www.seab.gov.sg/home/examinations/gce-o-level',
        1,
        GETUTCDATE(),
        NULL,
        155
    ),
    
    -- A-Level (Junior College)
    (
        NEWID(),
        'SG-ALEVEL',
        'Singapore-Cambridge GCE A-Level',
        '11,12',
        'https://www.seab.gov.sg/home/examinations/gce-a-level',
        1,
        GETUTCDATE(),
        NULL,
        190
    ),
    
    -- Integrated Programme (IP)
    (
        NEWID(),
        'SG-IP',
        'Singapore Integrated Programme (IP)',
        '9,10,11,12',
        'https://www.moe.gov.sg/education/programmes/integrated-programme',
        1,
        GETUTCDATE(),
        NULL,
        200
    );

-- GERMAN PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- German Abitur
    (
        NEWID(),
        'DE-ABITUR',
        'German Abitur (University Entrance Qualification)',
        '10,11,12,13',
        'https://www.kmk.org/themen/allgemeinbildende-schulen/bildungswege-und-abschluesse/sekundarstufe-ii-gymnasiale-oberstufe-und-abitur.html',
        1,
        GETUTCDATE(),
        NULL,
        195
    ),
    
    -- German International Abitur (DIA)
    (
        NEWID(),
        'DE-DIA',
        'Deutsches Internationales Abitur (DIA)',
        '10,11,12',
        'https://www.kmk.org/themen/deutsches-auslandsschulwesen.html',
        1,
        GETUTCDATE(),
        NULL,
        190
    );

-- CANADIAN PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Ontario Secondary School Diploma (OSSD)
    (
        NEWID(),
        'CA-OSSD',
        'Ontario Secondary School Diploma (OSSD)',
        '9,10,11,12',
        'https://www.ontario.ca/page/high-school-graduation-requirements',
        1,
        GETUTCDATE(),
        NULL,
        170
    ),
    
    -- British Columbia Dogwood Diploma
    (
        NEWID(),
        'CA-BC-DOGWOOD',
        'British Columbia Dogwood Diploma',
        '10,11,12',
        'https://www2.gov.bc.ca/gov/content/education-training/k-12/administration/program-management/graduation',
        1,
        GETUTCDATE(),
        NULL,
        165
    );

-- SPECIALIZED HIGH SCHOOL PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Advanced Mathematics Track
    (
        NEWID(),
        'INTL-ADVANCED-MATH',
        'International Advanced Mathematics Track',
        '9,10,11,12',
        'https://www.mathadvanced.org',
        1,
        GETUTCDATE(),
        NULL,
        210
    ),
    
    -- STEM Excellence Programme
    (
        NEWID(),
        'INTL-STEM-EXCELLENCE',
        'International STEM Excellence Programme',
        '9,10,11,12',
        'https://www.stem.org.uk/secondary',
        1,
        GETUTCDATE(),
        NULL,
        205
    ),
    
    -- International Mathematics Olympiad Preparation
    (
        NEWID(),
        'IMO-PREP',
        'International Mathematics Olympiad Preparation Track',
        '9,10,11,12',
        'https://www.imo-official.org/',
        1,
        GETUTCDATE(),
        NULL,
        220
    );

-- VIETNAM HIGH SCHOOL PROGRAMMES (For Comparison)
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Vietnam High School
    (
        '11111111-1111-1111-1111-111111111111',
        'VN-MOET-HIGHSCHOOL',
        N'Chương trình THPT Việt Nam (2018)',
        '10,11,12',
        'https://moet.gov.vn/giao-duc-thpt',
        1,
        GETUTCDATE(),
        NULL,
        150
    ),
    
    -- Vietnam Specialized High School (Math Focus)
    (
        NEWID(),
        'VN-SPECIALIZED-MATH',
        N'Chương trình THPT chuyên Toán',
        '10,11,12',
        'https://moet.gov.vn/truong-chuyen',
        1,
        GETUTCDATE(),
        NULL,
        180
    );

-- INACTIVE/LEGACY PROGRAMMES
INSERT INTO Curricula (CurriculumId, CurriculumCode, CurriculumName, Grades, SyllabusUrl, IsActive, CreatedDate, UpdatedDate, TotalCredits)
VALUES
    -- Old UK AS Level (Pre-Reform)
    (
        NEWID(),
        'UK-AS-OLD',
        'UK AS Level (Pre-2015 Reform)',
        '11',
        NULL,
        0,
        GETUTCDATE(),
        GETUTCDATE(),
        90
    ),
    
    -- SAT Subject Tests (Discontinued 2021)
    (
        NEWID(),
        'US-SAT-SUBJECT',
        'SAT Subject Tests (Discontinued)',
        '10,11,12',
        NULL,
        0,
        GETUTCDATE(),
        GETUTCDATE(),
        80
    );

-- ============================================
-- STEP 3: Verify Inserted Data
-- ============================================

-- Total count
SELECT COUNT(*) AS TotalCurricula FROM Curricula;

-- Count by status
SELECT 
    IsActive,
    COUNT(*) AS Count,
    CONCAT(CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Curricula) AS DECIMAL(5,2)), '%') AS Percentage
FROM Curricula
GROUP BY IsActive;

-- List all high school curricula
SELECT 
    CurriculumCode,
    CurriculumName,
    Grades,
    TotalCredits,
    IsActive,
    CASE 
        WHEN CurriculumCode LIKE 'UK-%' THEN 'United Kingdom'
        WHEN CurriculumCode LIKE 'IB-%' THEN 'International Baccalaureate'
        WHEN CurriculumCode LIKE 'US-%' THEN 'United States'
        WHEN CurriculumCode LIKE 'FR-%' THEN 'France'
        WHEN CurriculumCode LIKE 'AU-%' THEN 'Australia'
        WHEN CurriculumCode LIKE 'SG-%' THEN 'Singapore'
        WHEN CurriculumCode LIKE 'DE-%' THEN 'Germany'
        WHEN CurriculumCode LIKE 'CA-%' THEN 'Canada'
        WHEN CurriculumCode LIKE 'VN-%' THEN 'Vietnam'
        ELSE 'International'
    END AS Country
FROM Curricula
ORDER BY IsActive DESC, Country, CurriculumCode;

-- Group by country/system
SELECT 
    CASE 
        WHEN CurriculumCode LIKE 'UK-%' THEN 'United Kingdom'
        WHEN CurriculumCode LIKE 'IB-%' THEN 'International Baccalaureate'
        WHEN CurriculumCode LIKE 'US-%' THEN 'United States'
        WHEN CurriculumCode LIKE 'FR-%' THEN 'France'
        WHEN CurriculumCode LIKE 'AU-%' THEN 'Australia'
        WHEN CurriculumCode LIKE 'SG-%' THEN 'Singapore'
        WHEN CurriculumCode LIKE 'DE-%' THEN 'Germany'
        WHEN CurriculumCode LIKE 'CA-%' THEN 'Canada'
        WHEN CurriculumCode LIKE 'VN-%' THEN 'Vietnam'
        ELSE 'International'
    END AS Country,
    COUNT(*) AS ProgramCount
FROM Curricula
WHERE IsActive = 1
GROUP BY 
    CASE 
        WHEN CurriculumCode LIKE 'UK-%' THEN 'United Kingdom'
        WHEN CurriculumCode LIKE 'IB-%' THEN 'International Baccalaureate'
        WHEN CurriculumCode LIKE 'US-%' THEN 'United States'
        WHEN CurriculumCode LIKE 'FR-%' THEN 'France'
        WHEN CurriculumCode LIKE 'AU-%' THEN 'Australia'
        WHEN CurriculumCode LIKE 'SG-%' THEN 'Singapore'
        WHEN CurriculumCode LIKE 'DE-%' THEN 'Germany'
        WHEN CurriculumCode LIKE 'CA-%' THEN 'Canada'
        WHEN CurriculumCode LIKE 'VN-%' THEN 'Vietnam'
        ELSE 'International'
    END
ORDER BY ProgramCount DESC;

-- Export key curriculum IDs for School script
SELECT 
    CurriculumCode,
    CAST(CurriculumId AS VARCHAR(36)) AS CurriculumGUID,
    CurriculumName
FROM Curricula
WHERE IsActive = 1
ORDER BY CurriculumCode;

-- Credits distribution
SELECT 
    MIN(TotalCredits) AS MinCredits,
    MAX(TotalCredits) AS MaxCredits,
    AVG(TotalCredits) AS AvgCredits
FROM Curricula
WHERE IsActive = 1;

-- ============================================
-- SUMMARY & NOTES
-- ============================================
/*
TOTAL CURRICULA: 30 (28 Active, 2 Inactive)

DISTRIBUTION BY COUNTRY/SYSTEM:
1. United Kingdom (Cambridge): 3 programmes
   - IGCSE (Grades 9-10)
   - A-Level (Grades 11-12)
   - Pre-U (Grades 11-12)

2. International Baccalaureate: 3 programmes
   - MYP (Grades 9-10)
   - Diploma Programme (Grades 11-12) ⭐
   - Career-related Programme (Grades 11-12)

3. United States: 3 programmes
   - Advanced Placement (AP) ⭐
   - High School Diploma
   - SAT-Aligned

4. France: 2 programmes
   - Baccalauréat
   - International Baccalauréat (OIB)

5. Australia: 3 programmes
   - Year 10
   - ATAR Programme
   - VCE

6. Singapore: 3 programmes
   - O-Level (Grades 9-10)
   - A-Level (Grades 11-12) ⭐
   - Integrated Programme

7. Germany: 2 programmes
   - Abitur
   - International Abitur (DIA)

8. Canada: 2 programmes
   - OSSD (Ontario)
   - Dogwood Diploma (BC)

9. Specialized International: 3 programmes
   - Advanced Math Track
   - STEM Excellence
   - IMO Preparation ⭐

10. Vietnam: 2 programmes
    - THPT Standard
    - THPT Specialized Math

FIXED GUIDs FOR REFERENCE:
- 11111111-1111-1111-1111-111111111111: VN-MOET-HIGHSCHOOL
- 22222222-2222-2222-2222-222222222222: UK-CAMBRIDGE-IGCSE
- 33333333-3333-3333-3333-333333333333: IB-DP (Diploma Programme)
- 44444444-4444-4444-4444-444444444444: SG-OLEVEL

CREDITS RANGE: 140-220 credits
Most prestigious programmes (IB-DP, IMO-PREP, SG-IP): 200+ credits

NEXT STEPS:
1. Run this script to populate Curricula table
2. Use the exported GUIDs to update Schools script
3. Create high school data in Schools table
*/
-- ============================================