<template>
    <Layout>
      <PageHeader :title="pageTitle" :items="breadcrumbItems" />
      <div class="container mt-4">
        <h1 class="mb-4">Monitor API Dashboard</h1>
  
        <!-- Navigation Tabs -->
        <ul class="nav nav-tabs">
          <li class="nav-item" @click="activeTab = 'caller'">
            <a class="nav-link" :class="{ active: activeTab === 'caller' }">API Caller</a>
          </li>
          <li class="nav-item" @click="activeTab = 'history'">
            <a class="nav-link" :class="{ active: activeTab === 'history' }">Call History</a>
          </li>
          <li class="nav-item" @click="activeTab = 'schedule'">
            <a class="nav-link" :class="{ active: activeTab === 'schedule' }">Schedule Config</a>
          </li>
        </ul>
  
        <!-- Tab Content -->
        <div class="tab-content mt-3">
          <!-- API Caller Tab -->
          <div v-if="activeTab === 'caller'">
            <div class="card">
              <div class="card-header">API Caller</div>
              <div class="card-body">
                <form @submit.prevent="callApi">
                  <div class="form-group">
                    <label for="apiUrl">API URL</label>
                    <input
                      type="text"
                      id="apiUrl"
                      v-model="apiUrl"
                      class="form-control"
                      placeholder="Enter API URL (ex: https://apitest2.dongthap.gov.vn/api/v1/TestAPI/get-all)"
                      required
                    />
                  </div>
                  <div class="form-group">
                    <label for="method">Method</label>
                    <select id="method" v-model="method" class="form-control" required>
                      <option value="GET">GET</option>
                      <option value="POST">POST</option>
                    </select>
                  </div>
                  <div class="form-group" v-if="method === 'POST'">
                    <label for="payload">Payload (JSON)</label>
                    <textarea
                      id="payload"
                      v-model="payload"
                      class="form-control"
                      placeholder='{"key": "value"}'
                    ></textarea>
                  </div>
                  <div class="form-group">
                    <label for="name">Name</label>
                    <input
                      type="text"
                      id="name"
                      v-model="name"
                      class="form-control"
                      placeholder="Enter API name"
                    />
                  </div>
                  <div class="form-group">
                    <label for="note">Note</label>
                    <input
                      type="text"
                      id="note"
                      v-model="note"
                      class="form-control"
                      placeholder="Enter note"
                    />
                  </div>
                  <button type="submit" class="btn btn-primary">Call API</button>
                </form>
              </div>
            </div>
          </div>
  
          <!-- Call History Tab -->
          <div v-if="activeTab === 'history'">
            <div class="card">
              <div class="card-header">API Call History</div>
              <div class="card-body">
                <table class="table table-striped table-bordered">
                  <thead class="thead-dark">
                    <tr>
                      <th>Time</th>
                      <th>API URL</th>
                      <th>Method</th>
                      <th>Name</th>
                      <th>Status</th>
                      <th>Status Code</th>
                      <th>Response</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="history in callHistories" :key="history.id">
                      <td>{{ formatDate(history.callTime) }}</td>
                      <td>{{ history.apiUrl }}</td>
                      <td>{{ history.method }}</td>
                      <td>{{ history.name }}</td>
                      <td>
                        <span :class="{ 'text-success': history.status === 'Success', 'text-danger': history.status === 'Failure' }">
                          {{ history.status }}
                        </span>
                      </td>
                      <td>{{ history.statusCode }}</td>
                      <td><pre>{{ history.response }}</pre></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
  
          <!-- Schedule Config Tab (Placeholder) -->
          <div v-if="activeTab === 'schedule'">
            <div class="card">
              <div class="card-header">Schedule Configuration</div>
              <div class="card-body">
                <p>This section is under development.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Layout>
  </template>
  
  <script>

  </script>
  
  <style scoped>
  .container {
    max-width: 1000px;
  }
  .nav-tabs .nav-link {
    cursor: pointer;
  }
  </style>
  