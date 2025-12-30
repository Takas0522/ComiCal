#!/usr/bin/env node

/**
 * Azurite Blob Storage Image Seeding Script
 * 
 * This script downloads sample images for the seed data comics and uploads them to Azurite.
 * It uses the same ISBNs as defined in seed.sql.
 * 
 * Usage: node seed-images.js
 * 
 * Environment Variables:
 * - STORAGE_CONNECTION_STRING: Azure Storage connection string (defaults to Azurite local)
 */

const { BlobServiceClient } = require('@azure/storage-blob');
const https = require('https');
const http = require('http');

// Azurite connection string (default for local development)
const STORAGE_CONNECTION_STRING = process.env.STORAGE_CONNECTION_STRING || 
  'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;';

const CONTAINER_NAME = '$web';

// ISBNs from seed.sql - these are the comics we want to seed images for
const SEED_ISBNS = [
  '9784758087735',
  '9784824206077',
  '9784065403488',
  '9784098638369',
  '9784098638352',
  '9784065404782',
  '9784098638345',
  '9784098638338',
  '9784098638321',
  '9784867208861',
  '9784098638314',
  '9784416619421',
  '9784098638307',
  '9784098638291',
  '9784867208663',
  '9784815627652',
  '9784098638284',
  '9784091436566',
  '9784065408087',
  '9784867208595'
];

// Placeholder image URL (1x1 transparent PNG)
// In production, these would be actual book cover images
const PLACEHOLDER_IMAGE_DATA = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
  'base64'
);

/**
 * Download image from URL
 */
async function downloadImage(url) {
  return new Promise((resolve, reject) => {
    const protocol = url.startsWith('https') ? https : http;
    
    protocol.get(url, (response) => {
      if (response.statusCode !== 200) {
        reject(new Error(`Failed to download image: ${response.statusCode}`));
        return;
      }

      const chunks = [];
      response.on('data', (chunk) => chunks.push(chunk));
      response.on('end', () => {
        const buffer = Buffer.concat(chunks);
        const contentType = response.headers['content-type'] || 'image/jpeg';
        resolve({ buffer, contentType });
      });
    }).on('error', reject);
  });
}

/**
 * Get file extension from content type
 */
function getExtensionFromContentType(contentType) {
  const typeMap = {
    'image/jpeg': '.jpg',
    'image/jpg': '.jpg',
    'image/png': '.png',
    'image/gif': '.gif',
    'image/webp': '.webp'
  };
  return typeMap[contentType] || '.jpg';
}

/**
 * Check if blob exists in container
 */
async function blobExists(containerClient, blobName) {
  try {
    const blobClient = containerClient.getBlobClient(blobName);
    await blobClient.getProperties();
    return true;
  } catch (error) {
    if (error.statusCode === 404) {
      return false;
    }
    throw error;
  }
}

/**
 * Check if any blob with ISBN prefix exists
 */
async function hasImageForIsbn(containerClient, isbn) {
  const extensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  
  for (const ext of extensions) {
    const blobName = `${isbn}${ext}`;
    if (await blobExists(containerClient, blobName)) {
      return true;
    }
  }
  
  return false;
}

/**
 * Upload image to blob storage
 */
async function uploadImage(containerClient, isbn, imageBuffer, contentType) {
  const extension = getExtensionFromContentType(contentType);
  const blobName = `${isbn}${extension}`;
  
  console.log(`  Uploading ${blobName}...`);
  
  const blockBlobClient = containerClient.getBlockBlobClient(blobName);
  await blockBlobClient.upload(imageBuffer, imageBuffer.length, {
    blobHTTPHeaders: {
      blobContentType: contentType
    }
  });
  
  console.log(`  ✓ Uploaded ${blobName}`);
}

/**
 * Main seeding function
 */
async function seedImages() {
  console.log('=== Azurite Image Seeding Script ===\n');
  console.log(`Container: ${CONTAINER_NAME}`);
  console.log(`ISBNs to seed: ${SEED_ISBNS.length}\n`);

  try {
    // Create BlobServiceClient
    const blobServiceClient = BlobServiceClient.fromConnectionString(STORAGE_CONNECTION_STRING);
    const containerClient = blobServiceClient.getContainerClient(CONTAINER_NAME);

    // Create container if it doesn't exist
    console.log('Checking container...');
    const containerExists = await containerClient.exists();
    if (!containerExists) {
      console.log('Creating container...');
      await containerClient.create({
        access: 'blob' // Public access for blobs
      });
      console.log('✓ Container created\n');
    } else {
      console.log('✓ Container exists\n');
    }

    // Process each ISBN
    let uploadedCount = 0;
    let skippedCount = 0;

    for (const isbn of SEED_ISBNS) {
      console.log(`Processing ISBN: ${isbn}`);
      
      // Check if image already exists
      if (await hasImageForIsbn(containerClient, isbn)) {
        console.log(`  ⊙ Image already exists, skipping\n`);
        skippedCount++;
        continue;
      }

      // Upload placeholder image
      // In production, you would fetch the actual image from an external source here
      // For now, we use a 1x1 transparent PNG as placeholder
      await uploadImage(containerClient, isbn, PLACEHOLDER_IMAGE_DATA, 'image/png');
      uploadedCount++;
      console.log('');
    }

    console.log('=== Summary ===');
    console.log(`Total ISBNs processed: ${SEED_ISBNS.length}`);
    console.log(`Images uploaded: ${uploadedCount}`);
    console.log(`Images skipped (already exist): ${skippedCount}`);
    console.log('\n✓ Image seeding completed successfully!');

  } catch (error) {
    console.error('Error seeding images:', error);
    process.exit(1);
  }
}

// Run the script
seedImages();
