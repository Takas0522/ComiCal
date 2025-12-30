#!/usr/bin/env node

/**
 * Advanced Azurite Blob Storage Image Seeding Script with Rakuten API
 * 
 * This script fetches actual book cover images from Rakuten Books API 
 * and uploads them to Azurite Blob Storage.
 * 
 * Usage: 
 *   RAKUTEN_APP_ID=your_app_id node seed-images-from-api.js
 * 
 * Environment Variables:
 * - RAKUTEN_APP_ID: Rakuten API Application ID (required)
 * - STORAGE_CONNECTION_STRING: Azure Storage connection string (defaults to Azurite local)
 */

const { BlobServiceClient } = require('@azure/storage-blob');
const https = require('https');
const http = require('http');

// Configuration
const RAKUTEN_APP_ID = process.env.RAKUTEN_APP_ID;
const STORAGE_CONNECTION_STRING = process.env.STORAGE_CONNECTION_STRING || 
  'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;';

const CONTAINER_NAME = '$web';

// ISBNs from seed.sql
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

/**
 * Fetch book data from Rakuten Books API
 */
async function fetchBookDataFromRakuten(isbn) {
  return new Promise((resolve, reject) => {
    if (!RAKUTEN_APP_ID) {
      reject(new Error('RAKUTEN_APP_ID environment variable is not set'));
      return;
    }

    const url = `https://app.rakuten.co.jp/services/api/BooksBook/Search/20170404?format=json&isbn=${isbn}&applicationId=${RAKUTEN_APP_ID}`;
    
    https.get(url, (response) => {
      if (response.statusCode !== 200) {
        reject(new Error(`Failed to fetch from Rakuten API: ${response.statusCode}`));
        return;
      }

      let data = '';
      response.on('data', (chunk) => data += chunk);
      response.on('end', () => {
        try {
          const json = JSON.parse(data);
          if (json.Items && json.Items.length > 0) {
            resolve(json.Items[0].Item);
          } else {
            resolve(null);
          }
        } catch (error) {
          reject(error);
        }
      });
    }).on('error', reject);
  });
}

/**
 * Download image from URL
 */
async function downloadImage(url) {
  return new Promise((resolve, reject) => {
    const protocol = url.startsWith('https') ? https : http;
    
    protocol.get(url, (response) => {
      // Handle redirects
      if (response.statusCode === 301 || response.statusCode === 302) {
        return downloadImage(response.headers.location)
          .then(resolve)
          .catch(reject);
      }

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
 * Check if any blob with ISBN prefix exists
 */
async function hasImageForIsbn(containerClient, isbn) {
  const extensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  
  for (const ext of extensions) {
    const blobName = `${isbn}${ext}`;
    try {
      const blobClient = containerClient.getBlobClient(blobName);
      await blobClient.getProperties();
      return true;
    } catch (error) {
      if (error.statusCode !== 404) {
        throw error;
      }
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
  
  console.log(`  Uploading ${blobName} (${imageBuffer.length} bytes)...`);
  
  const blockBlobClient = containerClient.getBlockBlobClient(blobName);
  await blockBlobClient.upload(imageBuffer, imageBuffer.length, {
    blobHTTPHeaders: {
      blobContentType: contentType
    }
  });
  
  console.log(`  ✓ Uploaded ${blobName}`);
}

/**
 * Process a single ISBN
 */
async function processIsbn(containerClient, isbn) {
  console.log(`\nProcessing ISBN: ${isbn}`);
  
  // Check if image already exists
  if (await hasImageForIsbn(containerClient, isbn)) {
    console.log(`  ⊙ Image already exists, skipping`);
    return { status: 'skipped', reason: 'exists' };
  }

  try {
    // Fetch book data from Rakuten
    console.log(`  Fetching book data from Rakuten API...`);
    const bookData = await fetchBookDataFromRakuten(isbn);
    
    if (!bookData) {
      console.log(`  ⚠ Book not found in Rakuten API`);
      return { status: 'skipped', reason: 'not_found' };
    }

    // Get image URL (prefer largeImageUrl)
    const imageUrl = bookData.largeImageUrl || bookData.mediumImageUrl || bookData.smallImageUrl;
    
    if (!imageUrl) {
      console.log(`  ⚠ No image URL available`);
      return { status: 'skipped', reason: 'no_image' };
    }

    console.log(`  Image URL: ${imageUrl}`);
    
    // Download image
    console.log(`  Downloading image...`);
    const { buffer, contentType } = await downloadImage(imageUrl);
    
    // Upload to Azurite
    await uploadImage(containerClient, isbn, buffer, contentType);
    
    return { status: 'uploaded' };
  } catch (error) {
    console.error(`  ✗ Error: ${error.message}`);
    return { status: 'error', error: error.message };
  }
}

/**
 * Main seeding function
 */
async function seedImagesFromApi() {
  console.log('=== Azurite Image Seeding Script (with Rakuten API) ===\n');
  
  if (!RAKUTEN_APP_ID) {
    console.error('Error: RAKUTEN_APP_ID environment variable is required');
    console.error('Usage: RAKUTEN_APP_ID=your_app_id node seed-images-from-api.js');
    process.exit(1);
  }
  
  console.log(`Container: ${CONTAINER_NAME}`);
  console.log(`ISBNs to seed: ${SEED_ISBNS.length}`);
  console.log(`Rakuten App ID: ${RAKUTEN_APP_ID.substring(0, 8)}...`);
  console.log('');

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
        access: 'blob'
      });
      console.log('✓ Container created');
    } else {
      console.log('✓ Container exists');
    }

    // Process each ISBN
    const results = {
      uploaded: 0,
      skipped_exists: 0,
      skipped_not_found: 0,
      skipped_no_image: 0,
      error: 0
    };

    for (const isbn of SEED_ISBNS) {
      const result = await processIsbn(containerClient, isbn);
      
      if (result.status === 'uploaded') {
        results.uploaded++;
      } else if (result.status === 'skipped') {
        if (result.reason === 'exists') results.skipped_exists++;
        else if (result.reason === 'not_found') results.skipped_not_found++;
        else if (result.reason === 'no_image') results.skipped_no_image++;
      } else if (result.status === 'error') {
        results.error++;
      }
      
      // Rate limiting - wait 200ms between requests
      await new Promise(resolve => setTimeout(resolve, 200));
    }

    console.log('\n=== Summary ===');
    console.log(`Total ISBNs processed: ${SEED_ISBNS.length}`);
    console.log(`Images uploaded: ${results.uploaded}`);
    console.log(`Images skipped (already exist): ${results.skipped_exists}`);
    console.log(`Books not found in API: ${results.skipped_not_found}`);
    console.log(`Books without images: ${results.skipped_no_image}`);
    console.log(`Errors: ${results.error}`);
    console.log('\n✓ Image seeding completed!');

  } catch (error) {
    console.error('\nError seeding images:', error);
    process.exit(1);
  }
}

// Run the script
seedImagesFromApi();
