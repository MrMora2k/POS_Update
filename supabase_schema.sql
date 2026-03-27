-- Create a table for licenses
CREATE TABLE public.licenses (
    key TEXT PRIMARY KEY,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    machine_id TEXT,
    is_active BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL,
    activated_at TIMESTAMP WITH TIME ZONE,
    plain_password TEXT, -- Added for Admin visibility (Security Risk acknowledged)
    max_devices INTEGER DEFAULT 1,
    device_ids TEXT -- Pipe-separated list of machine IDs
);

-- Enable Row Level Security (RLS)
ALTER TABLE public.licenses ENABLE ROW LEVEL SECURITY;

-- Create policy to allow anyone to read licenses (Locked down in real app, but open for initial dev)
-- Ideally, you should only allow reading the specific key being activated
CREATE POLICY "Allow public read access" ON public.licenses
    FOR SELECT USING (true);

-- Create policy to allow update (activation)
CREATE POLICY "Allow public update access" ON public.licenses
    FOR UPDATE USING (true);

-- Create policy to allow insert (for the Key Generator App)
CREATE POLICY "Allow public insert access" ON public.licenses
    FOR INSERT WITH CHECK (true);

-- Create policy to allow delete (for the Key Generator App)
CREATE POLICY "Allow public delete access" ON public.licenses
    FOR DELETE USING (true);
