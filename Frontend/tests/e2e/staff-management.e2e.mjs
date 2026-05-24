import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import { once } from 'node:events'
import { setTimeout as delay } from 'node:timers/promises'
import test from 'node:test'

const frontendUrl = 'http://127.0.0.1:5173'
const backendUrl = 'http://localhost:5137'

test('staff management end-to-end flow through frontend proxy', async () => {
  const backend = startProcess(
    'dotnet',
    ['run', '--no-build', '--urls', backendUrl],
    '../Backend/StaffManagement',
  )

  const frontend = startProcess(
    'npm',
    ['run', 'dev', '--', '--host', '127.0.0.1'],
    '.',
  )

  try {
    await waitForOk(`${frontendUrl}/`)
    await waitForOk(`${frontendUrl}/api/Staffs`)

    const pageResponse = await fetch(`${frontendUrl}/`)
    const pageHtml = await pageResponse.text()
    assert.match(pageHtml, /\/src\/main\.jsx/)

    const name = `E2E Staff ${randomUUID().slice(0, 8)}`
    const createResponse = await fetch(`${frontendUrl}/api/Staffs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fullName: name,
        birthday: '2000-01-15',
        gender: 1,
      }),
    })

    assert.equal(createResponse.status, 201)
    const createdStaff = await createResponse.json()
    assert.match(createdStaff.staffId, /^STF\d{5}$/)
    assert.equal(createdStaff.fullName, name)

    const searchResponse = await fetch(`${frontendUrl}/api/Staffs/search?staffId=${createdStaff.staffId}`)
    assert.equal(searchResponse.status, 200)
    const searchResults = await searchResponse.json()
    assert.equal(searchResults.length, 1)
    assert.equal(searchResults[0].staffId, createdStaff.staffId)

    const updateResponse = await fetch(`${frontendUrl}/api/Staffs/${createdStaff.staffId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fullName: `${name} Updated`,
        birthday: '2001-02-16',
        gender: 2,
      }),
    })

    assert.equal(updateResponse.status, 204)

    const excelResponse = await fetch(`${frontendUrl}/api/Staffs/export/excel?staffId=${createdStaff.staffId}`)
    assert.equal(excelResponse.status, 200)
    assert.match(excelResponse.headers.get('content-type') ?? '', /application\/vnd\.ms-excel/)

    const pdfResponse = await fetch(`${frontendUrl}/api/Staffs/export/pdf?staffId=${createdStaff.staffId}`)
    assert.equal(pdfResponse.status, 200)
    assert.match(pdfResponse.headers.get('content-type') ?? '', /application\/pdf/)

    const deleteResponse = await fetch(`${frontendUrl}/api/Staffs/${createdStaff.staffId}`, {
      method: 'DELETE',
    })

    assert.equal(deleteResponse.status, 204)
  } finally {
    stopProcess(frontend)
    stopProcess(backend)
  }
})

function startProcess(command, args, cwd) {
  const child = spawn(command, args, {
    cwd,
    stdio: 'ignore',
    shell: false,
  })

  child.once('error', (error) => {
    throw error
  })

  return child
}

async function waitForOk(url) {
  const startedAt = Date.now()
  let lastError

  while (Date.now() - startedAt < 30000) {
    try {
      const response = await fetch(url)

      if (response.ok) {
        return
      }

      lastError = new Error(`${url} returned ${response.status}`)
    } catch (error) {
      lastError = error
    }

    await delay(500)
  }

  throw lastError ?? new Error(`${url} was not ready`)
}

async function stopProcess(child) {
  if (child.exitCode !== null) {
    return
  }

  child.kill('SIGTERM')

  await Promise.race([
    once(child, 'exit'),
    delay(5000).then(() => child.kill('SIGKILL')),
  ])
}
