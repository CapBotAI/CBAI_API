-- Enable extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";

-- Bảng roles
CREATE TABLE roles (
    role_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng users
CREATE TABLE users (
    user_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng user_roles
CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(role_id) ON DELETE CASCADE,
    PRIMARY KEY(user_id, role_id)
);

-- Bảng departments
CREATE TABLE departments (
    department_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng lecturers
CREATE TABLE lecturers (
    lecturer_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    full_name VARCHAR(255) NOT NULL,
    department_id UUID REFERENCES departments(department_id),
    email VARCHAR(255),
    phone VARCHAR(50),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng semesters
CREATE TABLE semesters (
    semester_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL CHECK (start_date <= end_date),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng phases
CREATE TABLE phases (
    phase_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    semester_id UUID NOT NULL REFERENCES semesters(semester_id) ON DELETE CASCADE,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    start_date DATE,
    end_date DATE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CHECK (start_date <= end_date)
);

-- Bảng phase_types
CREATE TABLE phase_types (
    phase_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    description TEXT
);

-- N-N phases <-> phase_types
CREATE TABLE phase_phase_type (
    phase_id UUID NOT NULL REFERENCES phases(phase_id) ON DELETE CASCADE,
    phase_type_id UUID NOT NULL REFERENCES phase_types(phase_type_id) ON DELETE CASCADE,
    PRIMARY KEY(phase_id, phase_type_id)
);

-- Bảng criteria
CREATE TABLE criteria (
    criteria_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    phase_type_id UUID NOT NULL REFERENCES phase_types(phase_type_id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    max_score INT NOT NULL CHECK (max_score > 0)
);

-- Bảng topics
CREATE TABLE topics (
    topic_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng topic_versions
CREATE TABLE topic_versions (
    topic_version_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    topic_id UUID NOT NULL REFERENCES topics(topic_id) ON DELETE CASCADE,
    version_number INT NOT NULL,
    content TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(topic_id, version_number)
);

-- Bảng topic_embeddings (dùng vector)
CREATE TABLE topic_embeddings (
    topic_embedding_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    topic_id UUID UNIQUE NOT NULL REFERENCES topics(topic_id) ON DELETE CASCADE,
    embedding_vector VECTOR(1536) NOT NULL, -- 1536 là chiều ví dụ, có thể điều chỉnh
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng ai_detections
CREATE TABLE ai_detections (
    ai_detection_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    topic_version_id UUID NOT NULL REFERENCES topic_versions(topic_version_id) ON DELETE CASCADE,
    detection_type VARCHAR(100) NOT NULL,
    detected_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng ai_detection_errors
CREATE TABLE ai_detection_errors (
    ai_detection_error_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ai_detection_id UUID NOT NULL REFERENCES ai_detections(ai_detection_id) ON DELETE CASCADE,
    error_code VARCHAR(50) NOT NULL,
    error_message TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Bảng reviews
CREATE TABLE reviews (
    review_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    topic_version_id UUID NOT NULL REFERENCES topic_versions(topic_version_id) ON DELETE CASCADE,
    phase_type_id UUID NOT NULL REFERENCES phase_types(phase_type_id) ON DELETE CASCADE,
    review_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    comments TEXT
);

-- Bảng review_lecturers
CREATE TABLE review_lecturers (
    review_id UUID NOT NULL REFERENCES reviews(review_id) ON DELETE CASCADE,
    lecturer_id UUID NOT NULL REFERENCES lecturers(lecturer_id) ON DELETE CASCADE,
    PRIMARY KEY(review_id, lecturer_id)
);

-- Bảng domains
CREATE TABLE domains (
    domain_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng technologies
CREATE TABLE technologies (
    technology_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng topic_categories
CREATE TABLE topic_categories (
    topic_category_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng application_types
CREATE TABLE application_types (
    application_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    description TEXT
);

-- Bảng topic_lecturers
CREATE TABLE topic_lecturers (
    topic_id UUID NOT NULL REFERENCES topics(topic_id) ON DELETE CASCADE,
    lecturer_id UUID NOT NULL REFERENCES lecturers(lecturer_id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL CHECK (role IN ('Supervisor', 'Reviewer', 'Moderator')),
    PRIMARY KEY(topic_id, lecturer_id, role)
);
