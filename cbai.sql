-- Enable extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";

CREATE TABLE departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE
);

-- Bảng Lecturer (Giảng viên)
CREATE TABLE lecturers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    department_id UUID NOT NULL REFERENCES departments(id) ON DELETE SET NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    phone VARCHAR(20),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Bảng Role (Vai trò)
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,  -- VD: supervisor, reviewer, moderator
    description TEXT
);

-- Bảng UserRoles (Giảng viên - Vai trò)
CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lecturer_id UUID NOT NULL REFERENCES lecturers(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    UNIQUE(lecturer_id, role_id)
);

-- Bảng Semester (Học kỳ)
CREATE TABLE semesters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    start_date DATE,
    end_date DATE
);

-- Bảng Topic (Đề tài)
CREATE TABLE topics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    semester_id UUID NOT NULL REFERENCES semesters(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Bảng TopicVersion (Phiên bản đề tài)
CREATE TABLE topic_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    version_number INT NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Bảng Domain (Lĩnh vực)
CREATE TABLE domains (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE
);

-- Bảng Technology (Công nghệ)
CREATE TABLE technologies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE
);

-- Bảng TopicCategory (Loại đề tài)
CREATE TABLE topic_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE
);

-- Bảng ApplicationType (Loại ứng dụng)
CREATE TABLE application_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE
);

-- Bảng liên kết nhiều-nhiều giữa Topic và Domain
CREATE TABLE topic_domain (
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    PRIMARY KEY(topic_id, domain_id)
);

-- Bảng liên kết nhiều-nhiều giữa Topic và Technology
CREATE TABLE topic_technology (
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    technology_id UUID NOT NULL REFERENCES technologies(id) ON DELETE CASCADE,
    PRIMARY KEY(topic_id, technology_id)
);

-- Bảng liên kết nhiều-nhiều giữa Topic và TopicCategory
CREATE TABLE topic_topic_category (
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    topic_category_id UUID NOT NULL REFERENCES topic_categories(id) ON DELETE CASCADE,
    PRIMARY KEY(topic_id, topic_category_id)
);

-- Bảng liên kết nhiều-nhiều giữa Topic và ApplicationType
CREATE TABLE topic_application_type (
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    application_type_id UUID NOT NULL REFERENCES application_types(id) ON DELETE CASCADE,
    PRIMARY KEY(topic_id, application_type_id)
);

-- Bảng TopicEmbedding (embedding vector cho Topic)
CREATE TABLE topic_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    topic_id UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    embedding_vector BYTEA NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Bảng Phase (Giai đoạn)
CREATE TABLE phases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    semester_id UUID NOT NULL REFERENCES semesters(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL
);

-- Bảng PhaseType (Loại giai đoạn)
CREATE TABLE phase_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL
);

-- Bảng liên kết nhiều-nhiều giữa Phase và PhaseType
CREATE TABLE phase_phase_type (
    phase_id UUID NOT NULL REFERENCES phases(id) ON DELETE CASCADE,
    phase_type_id UUID NOT NULL REFERENCES phase_types(id) ON DELETE CASCADE,
    PRIMARY KEY(phase_id, phase_type_id)
);

-- Bảng Criteria (Tiêu chí đánh giá)
CREATE TABLE criteria (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phase_type_id UUID NOT NULL REFERENCES phase_types(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT
);

-- Bảng Review (Đánh giá)
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phase_id UUID NOT NULL REFERENCES phases(id) ON DELETE CASCADE,
    phase_type_id UUID NOT NULL REFERENCES phase_types(id) ON DELETE CASCADE,
    topic_version_id UUID NOT NULL REFERENCES topic_versions(id) ON DELETE CASCADE,
    review_date TIMESTAMP DEFAULT NOW()
);

-- Bảng liên kết nhiều-nhiều giữa Review và Lecturer (người đánh giá)
CREATE TABLE review_lecturer (
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    lecturer_id UUID NOT NULL REFERENCES lecturers(id) ON DELETE CASCADE,
    PRIMARY KEY(review_id, lecturer_id)
);

-- Bảng AiDetectionErrors (danh sách lỗi AI có thể phát hiện)
CREATE TABLE ai_detection_errors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    error_code VARCHAR(50) UNIQUE NOT NULL,
    error_description TEXT NOT NULL
);

-- Bảng AiDetections (lưu các lỗi phát hiện trên từng topic_version)
CREATE TABLE ai_detections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    topic_version_id UUID NOT NULL REFERENCES topic_versions(id) ON DELETE CASCADE,
    ai_detection_error_id UUID NOT NULL REFERENCES ai_detection_errors(id) ON DELETE CASCADE,
    detected_at TIMESTAMP DEFAULT NOW(),
    details TEXT,
    severity_level INT,
    UNIQUE(topic_version_id, ai_detection_error_id)
);